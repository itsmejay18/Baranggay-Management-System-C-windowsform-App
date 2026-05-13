using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace baranggaysystem1
{
    public partial class Form1
    {
        private sealed class Form1Controller
        {
            private readonly Form1 _form;

            public Form1Controller(Form1 form)
            {
                _form = form;
            }

            public void HandleLogin()
            {
                string username = _form.txtUsername.Text;
                string password = _form.txtPassword.Text;

                if (OfflineDatabaseSupport.IsOffline)
                {
                    if (OfflineDatabaseSupport.TryAuthenticateOffline(
                        username,
                        password,
                        out int offlineUserId,
                        out int offlineBarangayId,
                        out string offlineRole))
                    {
                        UserSession.UserId = offlineUserId;
                        UserSession.BarangayId = offlineBarangayId;
                        UserSession.Role = offlineRole;
                        UserSession.Username = username;
                        Permissions.Refresh();

                        if (UserSession.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                            || UserSession.Role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
                        {
                            _form.CompleteLogin(new AdminDashboard());
                        }
                        else
                        {
                            _form.CompleteLogin(new StaffDashboard());
                        }

                        return;
                    }

                    ControllerDialogs.Warning("Invalid username or password");
                    return;
                }

                string query = @"SELECT ua.user_id,
                                        ua.barangay_id,
                                        COALESCE(r.name, 'Staff') AS role,
                                        ua.password_hash
                     FROM user_account ua
                     LEFT JOIN user_role ur ON ur.user_id = ua.user_id
                     LEFT JOIN role r ON r.role_id = ur.role_id
                     WHERE ua.username=@username
                     AND ua.is_active=1
                     ORDER BY
                        (r.name = 'Super Admin') DESC,
                        (r.name = 'Admin') DESC
                     LIMIT 1";

                using (MySqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);

                    using var reader = cmd.ExecuteReader();

                    if (!reader.Read())
                    {
                        ControllerDialogs.Warning("Invalid username or password");
                        return;
                    }

                    int userId = Convert.ToInt32(reader["user_id"]);
                    int barangayId = reader["barangay_id"] == DBNull.Value
                        ? SchemaDefaults.DefaultBarangayId
                        : Convert.ToInt32(reader["barangay_id"]);
                    string role = Convert.ToString(reader["role"]) ?? string.Empty;
                    string storedHash = Convert.ToString(reader["password_hash"]) ?? string.Empty;
                    reader.Close();

                    var verification = PasswordHelper.VerifyPassword(password, storedHash, out string? upgradedHash);
                    if (verification == PasswordHelper.VerificationResult.Failed)
                    {
                        ControllerDialogs.Warning("Invalid username or password");
                        return;
                    }

                    if (verification == PasswordHelper.VerificationResult.SuccessRehashNeeded
                        && !string.IsNullOrWhiteSpace(upgradedHash))
                    {
                        TryUpgradePasswordHash(conn, userId, upgradedHash);
                    }

                    UserSession.UserId = userId;
                    UserSession.BarangayId = barangayId;
                    UserSession.Role = role;
                    UserSession.Username = username;
                    Permissions.Refresh();

                    if (UserSession.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                        || UserSession.Role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        _form.CompleteLogin(new AdminDashboard());
                    }
                    else
                    {
                        _form.CompleteLogin(new StaffDashboard());
                    }
                }
            }

            public void HandleRegister()
            {
                _form.OpenRegister();
            }

            public void HandleLoad()
            {
                _form.StartFadeIn();
            }

            private static void TryUpgradePasswordHash(MySqlConnection conn, int userId, string upgradedHash)
            {
                try
                {
                    using var update = new MySqlCommand(
                        @"UPDATE user_account
                          SET password_hash = @hash,
                              updated_at = NOW()
                          WHERE user_id = @userId",
                        conn);
                    update.Parameters.AddWithValue("@hash", upgradedHash);
                    update.Parameters.AddWithValue("@userId", userId);
                    update.ExecuteNonQuery();
                }
                catch
                {
                    // Ignore hash upgrade failures (login already succeeded).
                }
            }
        }
    }
}
