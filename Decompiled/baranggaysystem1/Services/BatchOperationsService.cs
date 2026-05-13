using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

/// <summary>
/// Provides batch operations for lists (residents, certificates, blotters).
/// Supports multi-select actions like batch approve, archive, export, and status updates.
/// </summary>
public static class BatchOperationsService
{
    // ─── Resident Batch Operations ───────────────────────────────────────

    /// <summary>
    /// Batch archive (soft-delete) multiple residents.
    /// </summary>
    public static BatchResult ArchiveResidents(IReadOnlyList<int> residentIds, string reason = "")
    {
        if (residentIds == null || residentIds.Count == 0)
            return BatchResult.Empty("No residents selected.");

        if (!Permissions.Has(PermissionKeys.DeleteResidents))
            return BatchResult.Denied("You do not have permission to archive residents.");

        return ExecuteBatch("Residents", "resident", residentIds,
            (conn, tx, id) =>
            {
                var cmd = new MySqlCommand(
                    @"UPDATE resident SET is_deleted = 1, status = 'ARCHIVED', updated_at = NOW() 
                      WHERE resident_id = @id AND IFNULL(is_deleted,0) = 0",
                    conn, tx);
                try
                {
                    cmd.Parameters.AddWithValue("@id", (object)id);
                    return ((DbCommand)(object)cmd).ExecuteNonQuery() > 0;
                }
                finally { ((IDisposable)cmd)?.Dispose(); }
            },
            $"Batch archive: {reason}");
    }

    /// <summary>
    /// Batch restore archived residents.
    /// </summary>
    public static BatchResult RestoreResidents(IReadOnlyList<int> residentIds)
    {
        if (residentIds == null || residentIds.Count == 0)
            return BatchResult.Empty("No residents selected.");

        if (!Permissions.Has(PermissionKeys.UpdateResidents))
            return BatchResult.Denied("You do not have permission to restore residents.");

        return ExecuteBatch("Residents", "resident", residentIds,
            (conn, tx, id) =>
            {
                var cmd = new MySqlCommand(
                    @"UPDATE resident SET is_deleted = 0, status = 'ACTIVE', updated_at = NOW() 
                      WHERE resident_id = @id AND is_deleted = 1",
                    conn, tx);
                try
                {
                    cmd.Parameters.AddWithValue("@id", (object)id);
                    return ((DbCommand)(object)cmd).ExecuteNonQuery() > 0;
                }
                finally { ((IDisposable)cmd)?.Dispose(); }
            },
            "Batch restore residents.");
    }

    /// <summary>
    /// Batch update resident status.
    /// </summary>
    public static BatchResult UpdateResidentStatus(IReadOnlyList<int> residentIds, string newStatus)
    {
        if (residentIds == null || residentIds.Count == 0)
            return BatchResult.Empty("No residents selected.");

        if (!Permissions.Has(PermissionKeys.UpdateResidents))
            return BatchResult.Denied("You do not have permission to update residents.");

        var validStatuses = new[] { "ACTIVE", "INACTIVE", "DECEASED", "TRANSFERRED" };
        if (!validStatuses.Contains(newStatus.ToUpperInvariant()))
            return BatchResult.Failure($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");

        return ExecuteBatch("Residents", "resident", residentIds,
            (conn, tx, id) =>
            {
                var cmd = new MySqlCommand(
                    @"UPDATE resident SET status = @status, updated_at = NOW() 
                      WHERE resident_id = @id AND IFNULL(is_deleted,0) = 0",
                    conn, tx);
                try
                {
                    cmd.Parameters.AddWithValue("@id", (object)id);
                    cmd.Parameters.AddWithValue("@status", (object)newStatus.ToUpperInvariant());
                    return ((DbCommand)(object)cmd).ExecuteNonQuery() > 0;
                }
                finally { ((IDisposable)cmd)?.Dispose(); }
            },
            $"Batch status update to {newStatus}.");
    }

    // ─── Certificate Batch Operations ────────────────────────────────────

    /// <summary>
    /// Batch approve certificate requests.
    /// </summary>
    public static BatchResult ApproveCertificates(IReadOnlyList<int> requestIds)
    {
        if (requestIds == null || requestIds.Count == 0)
            return BatchResult.Empty("No certificate requests selected.");

        if (!Permissions.Has(PermissionKeys.ApproveCertificates))
            return BatchResult.Denied("You do not have permission to approve certificates.");

        return ExecuteBatch("Certificates", "document_request", requestIds,
            (conn, tx, id) =>
            {
                var cmd = new MySqlCommand(
                    @"UPDATE document_request 
                      SET status = 'APPROVED', approved_at = NOW(), approved_by = @userId, updated_at = NOW()
                      WHERE doc_request_id = @id AND status = 'SUBMITTED'",
                    conn, tx);
                try
                {
                    cmd.Parameters.AddWithValue("@id", (object)id);
                    cmd.Parameters.AddWithValue("@userId", (object)UserSession.UserId);
                    return ((DbCommand)(object)cmd).ExecuteNonQuery() > 0;
                }
                finally { ((IDisposable)cmd)?.Dispose(); }
            },
            "Batch certificate approval.");
    }

    /// <summary>
    /// Batch release approved certificates.
    /// </summary>
    public static BatchResult ReleaseCertificates(IReadOnlyList<int> requestIds)
    {
        if (requestIds == null || requestIds.Count == 0)
            return BatchResult.Empty("No certificate requests selected.");

        if (!Permissions.Has(PermissionKeys.IssueCertificates))
            return BatchResult.Denied("You do not have permission to release certificates.");

        return ExecuteBatch("Certificates", "document_request", requestIds,
            (conn, tx, id) =>
            {
                var cmd = new MySqlCommand(
                    @"UPDATE document_request 
                      SET status = 'RELEASED', released_at = NOW(), released_by = @userId, updated_at = NOW()
                      WHERE doc_request_id = @id AND status = 'APPROVED'",
                    conn, tx);
                try
                {
                    cmd.Parameters.AddWithValue("@id", (object)id);
                    cmd.Parameters.AddWithValue("@userId", (object)UserSession.UserId);
                    return ((DbCommand)(object)cmd).ExecuteNonQuery() > 0;
                }
                finally { ((IDisposable)cmd)?.Dispose(); }
            },
            "Batch certificate release.");
    }

    /// <summary>
    /// Batch cancel certificate requests.
    /// </summary>
    public static BatchResult CancelCertificates(IReadOnlyList<int> requestIds, string reason = "")
    {
        if (requestIds == null || requestIds.Count == 0)
            return BatchResult.Empty("No certificate requests selected.");

        if (!Permissions.Has(PermissionKeys.CancelCertificates))
            return BatchResult.Denied("You do not have permission to cancel certificates.");

        return ExecuteBatch("Certificates", "document_request", requestIds,
            (conn, tx, id) =>
            {
                var cmd = new MySqlCommand(
                    @"UPDATE document_request 
                      SET status = 'CANCELLED', cancelled_at = NOW(), cancel_reason = @reason, updated_at = NOW()
                      WHERE doc_request_id = @id AND status IN ('SUBMITTED', 'APPROVED')",
                    conn, tx);
                try
                {
                    cmd.Parameters.AddWithValue("@id", (object)id);
                    cmd.Parameters.AddWithValue("@reason", string.IsNullOrWhiteSpace(reason) ? DBNull.Value : (object)reason.Trim());
                    return ((DbCommand)(object)cmd).ExecuteNonQuery() > 0;
                }
                finally { ((IDisposable)cmd)?.Dispose(); }
            },
            $"Batch certificate cancellation: {reason}");
    }

    // ─── Blotter Batch Operations ────────────────────────────────────────

    /// <summary>
    /// Batch update blotter case status.
    /// </summary>
    public static BatchResult UpdateBlotterStatus(IReadOnlyList<int> caseIds, string newStatus)
    {
        if (caseIds == null || caseIds.Count == 0)
            return BatchResult.Empty("No blotter cases selected.");

        if (!Permissions.Has(PermissionKeys.UpdateBlotterStatus))
            return BatchResult.Denied("You do not have permission to update blotter status.");

        var validStatuses = new[] { "ONGOING", "SETTLED", "REFERRED", "CLOSED" };
        if (!validStatuses.Contains(newStatus.ToUpperInvariant()))
            return BatchResult.Failure($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");

        return ExecuteBatch("Blotter", "case_record", caseIds,
            (conn, tx, id) =>
            {
                var cmd = new MySqlCommand(
                    @"UPDATE case_record 
                      SET status = @status, updated_at = NOW()
                      WHERE case_id = @id",
                    conn, tx);
                try
                {
                    cmd.Parameters.AddWithValue("@id", (object)id);
                    cmd.Parameters.AddWithValue("@status", (object)newStatus.ToUpperInvariant());
                    return ((DbCommand)(object)cmd).ExecuteNonQuery() > 0;
                }
                finally { ((IDisposable)cmd)?.Dispose(); }
            },
            $"Batch blotter status update to {newStatus}.");
    }

    // ─── Export Batch Operations ─────────────────────────────────────────

    /// <summary>
    /// Get selected resident IDs as a comma-separated list for export filtering.
    /// </summary>
    public static string BuildIdFilter(IReadOnlyList<int> ids)
    {
        if (ids == null || ids.Count == 0) return "0";
        return string.Join(",", ids.Distinct());
    }

    /// <summary>
    /// Batch export selected residents to CSV.
    /// </summary>
    public static string ExportResidentsToCsv(IReadOnlyList<int> residentIds, string outputPath)
    {
        if (residentIds == null || residentIds.Count == 0)
            throw new ArgumentException("No residents selected for export.");

        string idList = BuildIdFilter(residentIds);
        var table = DbHelper.LoadTable(
            $@"SELECT resident_id, first_name, middle_name, last_name, suffix,
                      gender, birthdate, civil_status, contact_no, email, address,
                      occupation, nationality, religion, status, date_registered
               FROM resident 
               WHERE resident_id IN ({idList}) AND IFNULL(is_deleted,0) = 0
               ORDER BY last_name, first_name");

        var sb = new StringBuilder();
        // Header
        var columns = new[] { "ID", "Last Name", "First Name", "Middle Name", "Suffix",
            "Gender", "Birthdate", "Civil Status", "Contact", "Email", "Address",
            "Occupation", "Nationality", "Religion", "Status", "Date Registered" };
        sb.AppendLine(string.Join(",", columns.Select(c => $"\"{c}\"")));

        // Data rows
        foreach (System.Data.DataRow row in table.Rows)
        {
            var values = new List<string>();
            foreach (System.Data.DataColumn col in table.Columns)
            {
                string val = row[col] == DBNull.Value ? "" : Convert.ToString(row[col]) ?? "";
                values.Add($"\"{val.Replace("\"", "\"\"")}\"");
            }
            sb.AppendLine(string.Join(",", values));
        }

        string filePath = System.IO.Path.Combine(outputPath,
            $"residents_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        System.IO.File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

        AppLogger.LogInfo($"Exported {table.Rows.Count} residents to CSV.");
        return filePath;
    }

    // ─── Core Batch Execution Engine ─────────────────────────────────────

    private static BatchResult ExecuteBatch(
        string module, string entityType, IReadOnlyList<int> ids,
        Func<MySqlConnection, MySqlTransaction, int, bool> action,
        string auditNote)
    {
        int succeeded = 0;
        int failed = 0;
        var failedIds = new List<int>();

        MySqlConnection connection = DBConnection.GetConnection();
        try
        {
            ((DbConnection)(object)connection).Open();
            MySqlTransaction tx = connection.BeginTransaction();
            try
            {
                foreach (int id in ids)
                {
                    try
                    {
                        if (action(connection, tx, id))
                            succeeded++;
                        else
                        {
                            failed++;
                            failedIds.Add(id);
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        failedIds.Add(id);
                        AppLogger.LogWarning($"Batch operation failed for {entityType} ID {id}.", ex);
                    }
                }

                if (succeeded > 0)
                {
                    ((DbTransaction)(object)tx).Commit();

                    AuditTrailService.Log(module, entityType, 0, "BATCH_OPERATION", null,
                        new { Succeeded = succeeded, Failed = failed, Ids = ids },
                        auditNote);
                }
                else
                {
                    ((DbTransaction)(object)tx).Rollback();
                }

                return new BatchResult
                {
                    IsSuccess = succeeded > 0,
                    SucceededCount = succeeded,
                    FailedCount = failed,
                    FailedIds = failedIds,
                    Message = BuildResultMessage(succeeded, failed, ids.Count)
                };
            }
            catch (Exception ex)
            {
                ((DbTransaction)(object)tx).Rollback();
                AppLogger.LogError("Batch operation transaction failed.", ex);
                return BatchResult.Failure($"Batch operation failed: {ex.Message}");
            }
        }
        finally
        {
            ((IDisposable)connection)?.Dispose();
        }
    }

    private static string BuildResultMessage(int succeeded, int failed, int total)
    {
        if (failed == 0)
            return $"All {succeeded} item(s) processed successfully.";
        if (succeeded == 0)
            return $"Operation failed for all {total} item(s).";
        return $"{succeeded} of {total} item(s) processed. {failed} failed.";
    }
}

/// <summary>
/// Result of a batch operation.
/// </summary>
public sealed class BatchResult
{
    public bool IsSuccess { get; set; }
    public int SucceededCount { get; set; }
    public int FailedCount { get; set; }
    public List<int> FailedIds { get; set; } = new();
    public string Message { get; set; } = string.Empty;

    public static BatchResult Empty(string message) =>
        new() { IsSuccess = false, Message = message };

    public static BatchResult Denied(string message) =>
        new() { IsSuccess = false, Message = message };

    public static BatchResult Failure(string message) =>
        new() { IsSuccess = false, Message = message };
}
