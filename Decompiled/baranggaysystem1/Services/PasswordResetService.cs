using System;
using System.Data.Common;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

/// <summary>
/// Provides password reset functionality via security questions or admin-initiated reset.
/// Supports token-based reset flow and security question verification.
/// </summary>
public static class PasswordResetService
{
    private const int TokenExpiryMinutes = 30;
    private const int MaxResetAttemptsPerDay = 5;

    /// <summary>
    /// Ensure the password_reset table exists.
    /// </summary>
    public static void EnsureSchema()
    {
        try
        {
            DbHelper.ExecuteNonQuery(@"
                CREATE TABLE IF NOT EXISTS password_reset (
                    reset_id        INT AUTO_INCREMENT PRIMARY KEY,
                    user_id         INT NOT NULL,
                    reset_token     VARCHAR(128) NOT NULL,
                    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    expires_at      DATETIME NOT NULL,
                    used_at         DATETIME NULL,
                    is_used         TINYINT NOT NULL DEFAULT 0,
                    initiated_by    INT NULL,
                    reset_method    VARCHAR(30) NOT NULL DEFAULT 'ADMIN',
                    INDEX idx_reset_token (reset_token),
                    INDEX idx_reset_user (user_id)
                );");

            DbHelper.ExecuteNonQuery(@"
                CREATE TABLE IF NOT EXISTS security_question (
                    sq_id           INT AUTO_INCREMENT PRIMARY KEY,
                    user_id         INT NOT NULL UNIQUE,
                    question_1      VARCHAR(255) NOT NULL,
                    answer_1_hash   VARCHAR(255) NOT NULL,
                    question_2      VARCHAR(255) NULL,
                    answer_2_hash   VARCHAR(255) NULL,
                    updated_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    INDEX idx_sq_user (user_id)
                );");
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("PasswordResetService.EnsureSchema failed.", ex);
        }
    }

    /// <summary>
    /// Admin-initiated password reset. Generates a temporary password.
    /// </summary>
    public static PasswordResetResult AdminReset(int targetUserId, int adminUserId)
    {
        if (targetUserId <= 0)
            return PasswordResetResult.Failure("Invalid user ID.");

        if (!Permissions.Has(PermissionKeys.ManageUsers))
            return PasswordResetResult.Failure("You do not have permission to reset passwords.");

        try
        {
            EnsureSchema();

            // Generate temporary password
            string tempPassword = GenerateTemporaryPassword();
            string hashedPassword = PasswordHelper.HashPassword(tempPassword);

            MySqlConnection connection = DBConnection.GetConnection();
            try
            {
                ((DbConnection)(object)connection).Open();
                MySqlTransaction tx = connection.BeginTransaction();
                try
                {
                    // Update the user's password
                    var updateCmd = new MySqlCommand(
                        @"UPDATE user_account 
                          SET password_hash = @hash, 
                              must_change_password = 1,
                              updated_at = NOW() 
                          WHERE user_id = @userId AND is_active = 1",
                        connection, tx);
                    try
                    {
                        updateCmd.Parameters.AddWithValue("@hash", (object)hashedPassword);
                        updateCmd.Parameters.AddWithValue("@userId", (object)targetUserId);
                        int affected = ((DbCommand)(object)updateCmd).ExecuteNonQuery();

                        if (affected == 0)
                        {
                            ((DbTransaction)(object)tx).Rollback();
                            return PasswordResetResult.Failure("User not found or inactive.");
                        }
                    }
                    finally
                    {
                        ((IDisposable)updateCmd)?.Dispose();
                    }

                    // Log the reset token
                    string token = GenerateResetToken();
                    var tokenCmd = new MySqlCommand(
                        @"INSERT INTO password_reset (user_id, reset_token, expires_at, is_used, used_at, initiated_by, reset_method)
                          VALUES (@userId, @token, @expires, 1, NOW(), @adminId, 'ADMIN')",
                        connection, tx);
                    try
                    {
                        tokenCmd.Parameters.AddWithValue("@userId", (object)targetUserId);
                        tokenCmd.Parameters.AddWithValue("@token", (object)token);
                        tokenCmd.Parameters.AddWithValue("@expires", (object)DateTime.UtcNow.AddMinutes(TokenExpiryMinutes));
                        tokenCmd.Parameters.AddWithValue("@adminId", (object)adminUserId);
                        ((DbCommand)(object)tokenCmd).ExecuteNonQuery();
                    }
                    finally
                    {
                        ((IDisposable)tokenCmd)?.Dispose();
                    }

                    ((DbTransaction)(object)tx).Commit();

                    AuditTrailService.Log("Users", "user_account", targetUserId, "PASSWORD_RESET",
                        null, new { InitiatedBy = adminUserId, Method = "ADMIN" },
                        "Admin-initiated password reset.");

                    return PasswordResetResult.Success(tempPassword,
                        "Password has been reset. The user must change their password on next login.");
                }
                catch
                {
                    ((DbTransaction)(object)tx).Rollback();
                    throw;
                }
            }
            finally
            {
                ((IDisposable)connection)?.Dispose();
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Admin password reset failed.", ex);
            return PasswordResetResult.Failure($"Reset failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Self-service password reset via security questions.
    /// </summary>
    public static PasswordResetResult ResetViaSecurityQuestions(
        string username, string answer1, string? answer2 = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return PasswordResetResult.Failure("Username is required.");

        try
        {
            EnsureSchema();

            // Check rate limiting
            if (IsRateLimited(username))
                return PasswordResetResult.Failure(
                    "Too many reset attempts today. Please contact an administrator.");

            MySqlConnection connection = DBConnection.GetConnection();
            try
            {
                ((DbConnection)(object)connection).Open();

                // Find user
                int userId = FindUserByUsername(connection, username);
                if (userId <= 0)
                    return PasswordResetResult.Failure("Username not found or account is inactive.");

                // Verify security questions
                if (!VerifySecurityAnswers(connection, userId, answer1, answer2))
                    return PasswordResetResult.Failure("Security answer(s) are incorrect.");

                // Generate temporary password
                string tempPassword = GenerateTemporaryPassword();
                string hashedPassword = PasswordHelper.HashPassword(tempPassword);

                MySqlTransaction tx = connection.BeginTransaction();
                try
                {
                    var updateCmd = new MySqlCommand(
                        @"UPDATE user_account 
                          SET password_hash = @hash, 
                              must_change_password = 1,
                              updated_at = NOW() 
                          WHERE user_id = @userId",
                        connection, tx);
                    try
                    {
                        updateCmd.Parameters.AddWithValue("@hash", (object)hashedPassword);
                        updateCmd.Parameters.AddWithValue("@userId", (object)userId);
                        ((DbCommand)(object)updateCmd).ExecuteNonQuery();
                    }
                    finally
                    {
                        ((IDisposable)updateCmd)?.Dispose();
                    }

                    string token = GenerateResetToken();
                    var tokenCmd = new MySqlCommand(
                        @"INSERT INTO password_reset (user_id, reset_token, expires_at, is_used, used_at, reset_method)
                          VALUES (@userId, @token, @expires, 1, NOW(), 'SECURITY_QUESTION')",
                        connection, tx);
                    try
                    {
                        tokenCmd.Parameters.AddWithValue("@userId", (object)userId);
                        tokenCmd.Parameters.AddWithValue("@token", (object)token);
                        tokenCmd.Parameters.AddWithValue("@expires", (object)DateTime.UtcNow.AddMinutes(TokenExpiryMinutes));
                        ((DbCommand)(object)tokenCmd).ExecuteNonQuery();
                    }
                    finally
                    {
                        ((IDisposable)tokenCmd)?.Dispose();
                    }

                    ((DbTransaction)(object)tx).Commit();

                    AuditTrailService.Log("Users", "user_account", userId, "PASSWORD_RESET",
                        null, new { Method = "SECURITY_QUESTION" },
                        "Self-service password reset via security questions.");

                    return PasswordResetResult.Success(tempPassword,
                        "Password has been reset. Please change your password after logging in.");
                }
                catch
                {
                    ((DbTransaction)(object)tx).Rollback();
                    throw;
                }
            }
            finally
            {
                ((IDisposable)connection)?.Dispose();
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Security question password reset failed.", ex);
            return PasswordResetResult.Failure($"Reset failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Set security questions for a user.
    /// </summary>
    public static bool SetSecurityQuestions(int userId, string question1, string answer1,
        string? question2 = null, string? answer2 = null)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(question1) || string.IsNullOrWhiteSpace(answer1))
            return false;

        try
        {
            EnsureSchema();

            string hash1 = PasswordHelper.HashPassword(answer1.Trim().ToLowerInvariant());
            string? hash2 = string.IsNullOrWhiteSpace(answer2)
                ? null
                : PasswordHelper.HashPassword(answer2.Trim().ToLowerInvariant());

            DbHelper.ExecuteNonQuery(
                @"INSERT INTO security_question (user_id, question_1, answer_1_hash, question_2, answer_2_hash, updated_at)
                  VALUES (@userId, @q1, @a1, @q2, @a2, NOW())
                  ON DUPLICATE KEY UPDATE
                    question_1 = @q1, answer_1_hash = @a1,
                    question_2 = @q2, answer_2_hash = @a2,
                    updated_at = NOW()",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@userId", (object)userId);
                    cmd.Parameters.AddWithValue("@q1", (object)question1.Trim());
                    cmd.Parameters.AddWithValue("@a1", (object)hash1);
                    cmd.Parameters.AddWithValue("@q2", string.IsNullOrWhiteSpace(question2) ? DBNull.Value : (object)question2.Trim());
                    cmd.Parameters.AddWithValue("@a2", hash2 == null ? DBNull.Value : (object)hash2);
                });

            return true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to set security questions.", ex);
            return false;
        }
    }

    /// <summary>
    /// Get security questions for a user (without answers).
    /// </summary>
    public static SecurityQuestionSet? GetSecurityQuestions(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;

        try
        {
            EnsureSchema();
            MySqlConnection connection = DBConnection.GetConnection();
            try
            {
                ((DbConnection)(object)connection).Open();
                int userId = FindUserByUsername(connection, username);
                if (userId <= 0) return null;

                var cmd = new MySqlCommand(
                    "SELECT question_1, question_2 FROM security_question WHERE user_id = @uid LIMIT 1",
                    connection);
                try
                {
                    cmd.Parameters.AddWithValue("@uid", (object)userId);
                    using var reader = cmd.ExecuteReader();
                    if (!((DbDataReader)(object)reader).Read()) return null;

                    return new SecurityQuestionSet
                    {
                        Question1 = Convert.ToString(((DbDataReader)(object)reader)["question_1"]) ?? "",
                        Question2 = Convert.ToString(((DbDataReader)(object)reader)["question_2"]) ?? ""
                    };
                }
                finally
                {
                    ((IDisposable)cmd)?.Dispose();
                }
            }
            finally
            {
                ((IDisposable)connection)?.Dispose();
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Check if a user has the must_change_password flag set.
    /// </summary>
    public static bool MustChangePassword(int userId)
    {
        if (userId <= 0) return false;
        try
        {
            int result = DbHelper.ExecuteScalar<int>(
                "SELECT IFNULL(must_change_password, 0) FROM user_account WHERE user_id = @id LIMIT 1",
                cmd => cmd.Parameters.AddWithValue("@id", (object)userId));
            return result == 1;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clear the must_change_password flag after user changes their password.
    /// </summary>
    public static void ClearMustChangePassword(int userId)
    {
        if (userId <= 0) return;
        try
        {
            DbHelper.ExecuteNonQuery(
                "UPDATE user_account SET must_change_password = 0, updated_at = NOW() WHERE user_id = @id",
                cmd => cmd.Parameters.AddWithValue("@id", (object)userId));
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to clear must_change_password flag.", ex);
        }
    }

    /// <summary>
    /// Change password for the current user (self-service).
    /// </summary>
    public static PasswordResetResult ChangePassword(int userId, string currentPassword, string newPassword)
    {
        if (userId <= 0)
            return PasswordResetResult.Failure("Invalid user.");
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return PasswordResetResult.Failure("New password must be at least 6 characters.");

        try
        {
            // Verify current password
            string storedHash = DbHelper.ExecuteScalar<string>(
                "SELECT password_hash FROM user_account WHERE user_id = @id AND is_active = 1 LIMIT 1",
                cmd => cmd.Parameters.AddWithValue("@id", (object)userId)) ?? "";

            if (string.IsNullOrEmpty(storedHash))
                return PasswordResetResult.Failure("User not found.");

            var verifyResult = PasswordHelper.VerifyPassword(currentPassword, storedHash, out _);
            if (verifyResult == PasswordHelper.VerificationResult.Failed)
                return PasswordResetResult.Failure("Current password is incorrect.");

            // Set new password
            string newHash = PasswordHelper.HashPassword(newPassword);
            DbHelper.ExecuteNonQuery(
                @"UPDATE user_account 
                  SET password_hash = @hash, must_change_password = 0, updated_at = NOW() 
                  WHERE user_id = @id",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@hash", (object)newHash);
                    cmd.Parameters.AddWithValue("@id", (object)userId);
                });

            AuditTrailService.Log("Users", "user_account", userId, "PASSWORD_CHANGE",
                null, null, "User changed their own password.");

            return PasswordResetResult.Success(null, "Password changed successfully.");
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Password change failed.", ex);
            return PasswordResetResult.Failure($"Password change failed: {ex.Message}");
        }
    }

    private static int FindUserByUsername(MySqlConnection conn, string username)
    {
        var cmd = new MySqlCommand(
            "SELECT user_id FROM user_account WHERE username = @u AND is_active = 1 LIMIT 1",
            conn);
        try
        {
            cmd.Parameters.AddWithValue("@u", (object)username.Trim());
            object result = ((DbCommand)(object)cmd).ExecuteScalar();
            return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }
        finally
        {
            ((IDisposable)cmd)?.Dispose();
        }
    }

    private static bool VerifySecurityAnswers(MySqlConnection conn, int userId, string answer1, string? answer2)
    {
        var cmd = new MySqlCommand(
            "SELECT answer_1_hash, answer_2_hash FROM security_question WHERE user_id = @uid LIMIT 1",
            conn);
        try
        {
            cmd.Parameters.AddWithValue("@uid", (object)userId);
            using var reader = cmd.ExecuteReader();
            if (!((DbDataReader)(object)reader).Read()) return false;

            string hash1 = Convert.ToString(((DbDataReader)(object)reader)["answer_1_hash"]) ?? "";
            string hash2 = Convert.ToString(((DbDataReader)(object)reader)["answer_2_hash"]) ?? "";

            // Verify answer 1
            var result1 = PasswordHelper.VerifyPassword(answer1.Trim().ToLowerInvariant(), hash1, out _);
            if (result1 == PasswordHelper.VerificationResult.Failed) return false;

            // Verify answer 2 if it exists
            if (!string.IsNullOrWhiteSpace(hash2) && !string.IsNullOrWhiteSpace(answer2))
            {
                var result2 = PasswordHelper.VerifyPassword(answer2.Trim().ToLowerInvariant(), hash2, out _);
                if (result2 == PasswordHelper.VerificationResult.Failed) return false;
            }

            return true;
        }
        finally
        {
            ((IDisposable)cmd)?.Dispose();
        }
    }

    private static bool IsRateLimited(string username)
    {
        try
        {
            int count = DbHelper.ExecuteScalar<int>(
                @"SELECT COUNT(*) FROM password_reset pr
                  INNER JOIN user_account ua ON ua.user_id = pr.user_id
                  WHERE ua.username = @u AND DATE(pr.created_at) = CURRENT_DATE()",
                cmd => cmd.Parameters.AddWithValue("@u", (object)username.Trim()));
            return count >= MaxResetAttemptsPerDay;
        }
        catch
        {
            return false;
        }
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        var bytes = new byte[10];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        var result = new char[10];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        return new string(result);
    }

    private static string GenerateResetToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

/// <summary>
/// Result of a password reset operation.
/// </summary>
public sealed class PasswordResetResult
{
    public bool IsSuccess { get; private init; }
    public string? TemporaryPassword { get; private init; }
    public string Message { get; private init; } = string.Empty;

    public static PasswordResetResult Success(string? tempPassword, string message) =>
        new() { IsSuccess = true, TemporaryPassword = tempPassword, Message = message };

    public static PasswordResetResult Failure(string message) =>
        new() { IsSuccess = false, Message = message };
}

/// <summary>
/// Security questions for a user (questions only, no answers).
/// </summary>
public sealed class SecurityQuestionSet
{
    public string Question1 { get; set; } = string.Empty;
    public string Question2 { get; set; } = string.Empty;
    public bool HasQuestion2 => !string.IsNullOrWhiteSpace(Question2);
}
