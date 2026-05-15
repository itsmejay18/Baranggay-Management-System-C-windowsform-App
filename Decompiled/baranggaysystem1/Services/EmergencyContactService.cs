using System;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

/// <summary>
/// Manages the barangay's emergency contact directory (police, fire, medical, utilities, etc.).
/// </summary>
internal sealed class EmergencyContactService
{
    public static readonly string[] Categories = new[]
    {
        "POLICE", "FIRE", "MEDICAL", "DISASTER", "UTILITY", "GOVERNMENT", "OTHER"
    };

    public async Task<DataTable> LoadAsync(string? searchText = null, string? category = null,
        bool priorityOnly = false, bool includeInactive = false)
    {
        string search = (searchText ?? string.Empty).Trim();
        string like = "%" + search + "%";
        string sql = @"SELECT contact_id,
                              category,
                              agency_name,
                              COALESCE(contact_person, '') AS contact_person,
                              phone_primary,
                              COALESCE(phone_secondary, '') AS phone_secondary,
                              COALESCE(email, '') AS email,
                              COALESCE(address, '') AS address,
                              COALESCE(notes, '') AS notes,
                              is_priority,
                              is_active,
                              updated_at
                         FROM emergency_contact
                        WHERE (@includeInactive = 1 OR is_active = 1)
                          AND (@priorityOnly = 0 OR is_priority = 1)
                          AND (@category = '' OR category = @category)
                          AND (@search = '' OR agency_name LIKE @like
                               OR COALESCE(contact_person, '') LIKE @like
                               OR phone_primary LIKE @like)
                        ORDER BY is_priority DESC, category, agency_name";
        return await DatabaseManagerAsync.LoadTableAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@search", (object)search);
            cmd.Parameters.AddWithValue("@like", (object)like);
            cmd.Parameters.AddWithValue("@category", (object)(category ?? string.Empty));
            cmd.Parameters.AddWithValue("@priorityOnly", (object)(priorityOnly ? 1 : 0));
            cmd.Parameters.AddWithValue("@includeInactive", (object)(includeInactive ? 1 : 0));
        }).ConfigureAwait(false);
    }

    public async Task<DataTable> GetAsync(int contactId)
    {
        return await DatabaseManagerAsync.LoadTableAsync(
            "SELECT * FROM emergency_contact WHERE contact_id = @id LIMIT 1",
            cmd => cmd.Parameters.AddWithValue("@id", (object)contactId)).ConfigureAwait(false);
    }

    public async Task<int> CreateAsync(string category, string agencyName, string? contactPerson,
        string phonePrimary, string? phoneSecondary, string? email, string? address,
        string? notes, bool isPriority)
    {
        const string sql = @"INSERT INTO emergency_contact
                               (category, agency_name, contact_person, phone_primary, phone_secondary,
                                email, address, notes, is_priority, is_active)
                             VALUES
                               (@category, @agency, @person, @phone1, @phone2, @email, @addr, @notes, @priority, 1)";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@category", (object)NormalizeCategory(category));
            cmd.Parameters.AddWithValue("@agency", (object)(agencyName ?? "").Trim());
            cmd.Parameters.AddWithValue("@person", DbNullIfEmpty(contactPerson));
            cmd.Parameters.AddWithValue("@phone1", (object)(phonePrimary ?? "").Trim());
            cmd.Parameters.AddWithValue("@phone2", DbNullIfEmpty(phoneSecondary));
            cmd.Parameters.AddWithValue("@email", DbNullIfEmpty(email));
            cmd.Parameters.AddWithValue("@addr", DbNullIfEmpty(address));
            cmd.Parameters.AddWithValue("@notes", DbNullIfEmpty(notes));
            cmd.Parameters.AddWithValue("@priority", (object)(isPriority ? 1 : 0));
        }).ConfigureAwait(false);

        var idTable = await DatabaseManagerAsync.LoadTableAsync(
            "SELECT contact_id FROM emergency_contact WHERE agency_name = @name ORDER BY contact_id DESC LIMIT 1",
            cmd => cmd.Parameters.AddWithValue("@name", (object)(agencyName ?? "").Trim())).ConfigureAwait(false);
        int id = idTable.Rows.Count > 0 ? Convert.ToInt32(idTable.Rows[0]["contact_id"]) : 0;
        AuditTrailService.Log("EmergencyContact", "emergency_contact", id, "CREATE", null,
            new { agencyName, category }, "Emergency contact added.");
        return id;
    }

    public async Task UpdateAsync(int contactId, string category, string agencyName,
        string? contactPerson, string phonePrimary, string? phoneSecondary, string? email,
        string? address, string? notes, bool isPriority, bool isActive)
    {
        const string sql = @"UPDATE emergency_contact
                                SET category = @category,
                                    agency_name = @agency,
                                    contact_person = @person,
                                    phone_primary = @phone1,
                                    phone_secondary = @phone2,
                                    email = @email,
                                    address = @addr,
                                    notes = @notes,
                                    is_priority = @priority,
                                    is_active = @active
                              WHERE contact_id = @id";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@id", (object)contactId);
            cmd.Parameters.AddWithValue("@category", (object)NormalizeCategory(category));
            cmd.Parameters.AddWithValue("@agency", (object)(agencyName ?? "").Trim());
            cmd.Parameters.AddWithValue("@person", DbNullIfEmpty(contactPerson));
            cmd.Parameters.AddWithValue("@phone1", (object)(phonePrimary ?? "").Trim());
            cmd.Parameters.AddWithValue("@phone2", DbNullIfEmpty(phoneSecondary));
            cmd.Parameters.AddWithValue("@email", DbNullIfEmpty(email));
            cmd.Parameters.AddWithValue("@addr", DbNullIfEmpty(address));
            cmd.Parameters.AddWithValue("@notes", DbNullIfEmpty(notes));
            cmd.Parameters.AddWithValue("@priority", (object)(isPriority ? 1 : 0));
            cmd.Parameters.AddWithValue("@active", (object)(isActive ? 1 : 0));
        }).ConfigureAwait(false);
        AuditTrailService.Log("EmergencyContact", "emergency_contact", contactId, "UPDATE", null,
            new { agencyName, category, isActive }, "Emergency contact updated.");
    }

    public async Task DeleteAsync(int contactId)
    {
        await DatabaseManagerAsync.ExecuteNonQueryAsync(
            "DELETE FROM emergency_contact WHERE contact_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", (object)contactId)).ConfigureAwait(false);
        AuditTrailService.Log("EmergencyContact", "emergency_contact", contactId, "DELETE", null, null,
            "Emergency contact removed.");
    }

    private static string NormalizeCategory(string? cat)
    {
        string c = (cat ?? "").Trim().ToUpperInvariant();
        return Array.IndexOf(Categories, c) >= 0 ? c : "OTHER";
    }

    private static object DbNullIfEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
        return value.Trim();
    }
}
