using System;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

/// <summary>
/// Manages barangay facilities and booking requests.
/// </summary>
internal sealed class FacilityBookingService
{
    public static readonly string[] FacilityTypes = new[] { "VENUE", "EQUIPMENT", "VEHICLE", "OTHER" };
    public static readonly string[] BookingStatuses = new[] { "PENDING", "APPROVED", "REJECTED", "COMPLETED", "CANCELLED" };
    public static readonly string[] PaymentStatuses = new[] { "UNPAID", "PARTIAL", "PAID", "WAIVED" };

    // ============================================================
    // FACILITIES
    // ============================================================

    public async Task<DataTable> LoadFacilitiesAsync(bool includeInactive = false)
    {
        string sql = @"SELECT facility_id,
                              facility_name,
                              facility_type,
                              capacity,
                              hourly_rate,
                              COALESCE(location, '') AS location,
                              COALESCE(description, '') AS description,
                              is_active
                         FROM barangay_facility
                        WHERE (@includeInactive = 1 OR is_active = 1)
                        ORDER BY facility_type, facility_name";
        return await DatabaseManagerAsync.LoadTableAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@includeInactive", (object)(includeInactive ? 1 : 0));
        }).ConfigureAwait(false);
    }

    public async Task<int> CreateFacilityAsync(string name, string type, int? capacity,
        decimal hourlyRate, string? location, string? description)
    {
        const string sql = @"INSERT INTO barangay_facility
                               (facility_name, facility_type, capacity, hourly_rate, location, description, is_active)
                             VALUES
                               (@name, @type, @cap, @rate, @loc, @desc, 1)";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@name", (object)(name ?? "").Trim());
            cmd.Parameters.AddWithValue("@type", (object)NormalizeFacilityType(type));
            cmd.Parameters.AddWithValue("@cap", capacity.HasValue ? (object)capacity.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@rate", (object)hourlyRate);
            cmd.Parameters.AddWithValue("@loc", DbNullIfEmpty(location));
            cmd.Parameters.AddWithValue("@desc", DbNullIfEmpty(description));
        }).ConfigureAwait(false);

        var idTable = await DatabaseManagerAsync.LoadTableAsync(
            "SELECT facility_id FROM barangay_facility WHERE facility_name = @name ORDER BY facility_id DESC LIMIT 1",
            cmd => cmd.Parameters.AddWithValue("@name", (object)(name ?? "").Trim())).ConfigureAwait(false);
        int id = idTable.Rows.Count > 0 ? Convert.ToInt32(idTable.Rows[0]["facility_id"]) : 0;
        AuditTrailService.Log("Facilities", "barangay_facility", id, "CREATE", null,
            new { name, type }, "Facility registered.");
        return id;
    }

    public async Task UpdateFacilityAsync(int facilityId, string name, string type, int? capacity,
        decimal hourlyRate, string? location, string? description, bool isActive)
    {
        const string sql = @"UPDATE barangay_facility
                                SET facility_name = @name,
                                    facility_type = @type,
                                    capacity = @cap,
                                    hourly_rate = @rate,
                                    location = @loc,
                                    description = @desc,
                                    is_active = @active
                              WHERE facility_id = @id";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@id", (object)facilityId);
            cmd.Parameters.AddWithValue("@name", (object)(name ?? "").Trim());
            cmd.Parameters.AddWithValue("@type", (object)NormalizeFacilityType(type));
            cmd.Parameters.AddWithValue("@cap", capacity.HasValue ? (object)capacity.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@rate", (object)hourlyRate);
            cmd.Parameters.AddWithValue("@loc", DbNullIfEmpty(location));
            cmd.Parameters.AddWithValue("@desc", DbNullIfEmpty(description));
            cmd.Parameters.AddWithValue("@active", (object)(isActive ? 1 : 0));
        }).ConfigureAwait(false);
        AuditTrailService.Log("Facilities", "barangay_facility", facilityId, "UPDATE", null,
            new { name, isActive }, "Facility updated.");
    }

    // ============================================================
    // BOOKINGS
    // ============================================================

    public async Task<DataTable> LoadBookingsAsync(string? searchText = null, string? statusFilter = null,
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        string search = (searchText ?? string.Empty).Trim();
        string like = "%" + search + "%";
        string sql = @"SELECT b.booking_id,
                              f.facility_name,
                              b.requester_name,
                              COALESCE(b.requester_contact, '') AS requester_contact,
                              b.purpose,
                              b.start_at,
                              b.end_at,
                              b.total_amount,
                              b.payment_status,
                              b.status
                         FROM facility_booking b
                         JOIN barangay_facility f ON f.facility_id = b.facility_id
                        WHERE (@search = '' OR b.requester_name LIKE @like OR b.purpose LIKE @like OR f.facility_name LIKE @like)
                          AND (@status = '' OR b.status = @status)
                          AND (@hasFrom = 0 OR b.start_at >= @from)
                          AND (@hasTo = 0 OR b.end_at <= @to)
                        ORDER BY b.start_at DESC";
        return await DatabaseManagerAsync.LoadTableAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@search", (object)search);
            cmd.Parameters.AddWithValue("@like", (object)like);
            cmd.Parameters.AddWithValue("@status", (object)(statusFilter ?? string.Empty));
            cmd.Parameters.AddWithValue("@hasFrom", (object)(fromDate.HasValue ? 1 : 0));
            cmd.Parameters.AddWithValue("@from", fromDate.HasValue ? (object)fromDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@hasTo", (object)(toDate.HasValue ? 1 : 0));
            cmd.Parameters.AddWithValue("@to", toDate.HasValue ? (object)toDate.Value : DBNull.Value);
        }).ConfigureAwait(false);
    }

    public async Task<bool> HasConflictAsync(int facilityId, DateTime startAt, DateTime endAt,
        int? excludeBookingId = null)
    {
        const string sql = @"SELECT COUNT(*)
                               FROM facility_booking
                              WHERE facility_id = @fid
                                AND status IN ('PENDING', 'APPROVED')
                                AND (@excludeId = 0 OR booking_id != @excludeId)
                                AND start_at < @end
                                AND end_at > @start";
        int count = await DatabaseManagerAsync.ExecuteScalarAsync<int>(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@fid", (object)facilityId);
            cmd.Parameters.AddWithValue("@start", (object)startAt);
            cmd.Parameters.AddWithValue("@end", (object)endAt);
            cmd.Parameters.AddWithValue("@excludeId", (object)(excludeBookingId ?? 0));
        }).ConfigureAwait(false);
        return count > 0;
    }

    public async Task<int> CreateBookingAsync(int facilityId, string requesterName, string? contact,
        string purpose, DateTime startAt, DateTime endAt, int? expectedGuests,
        decimal totalAmount, int? residentId)
    {
        if (startAt >= endAt)
            throw new InvalidOperationException("End time must be later than start time.");

        if (await HasConflictAsync(facilityId, startAt, endAt).ConfigureAwait(false))
            throw new InvalidOperationException("This facility is already booked for the selected time range.");

        const string sql = @"INSERT INTO facility_booking
                               (facility_id, resident_id, requester_name, requester_contact,
                                purpose, start_at, end_at, expected_guests, total_amount,
                                payment_status, status)
                             VALUES
                               (@fid, @rid, @name, @contact, @purpose, @start, @end,
                                @guests, @amount, 'UNPAID', 'PENDING')";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@fid", (object)facilityId);
            cmd.Parameters.AddWithValue("@rid", residentId.HasValue && residentId.Value > 0
                ? (object)residentId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@name", (object)(requesterName ?? "").Trim());
            cmd.Parameters.AddWithValue("@contact", DbNullIfEmpty(contact));
            cmd.Parameters.AddWithValue("@purpose", (object)(purpose ?? "").Trim());
            cmd.Parameters.AddWithValue("@start", (object)startAt);
            cmd.Parameters.AddWithValue("@end", (object)endAt);
            cmd.Parameters.AddWithValue("@guests", expectedGuests.HasValue ? (object)expectedGuests.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@amount", (object)totalAmount);
        }).ConfigureAwait(false);

        var idTable = await DatabaseManagerAsync.LoadTableAsync(
            @"SELECT booking_id FROM facility_booking
              WHERE facility_id = @fid AND requester_name = @name
              ORDER BY booking_id DESC LIMIT 1",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@fid", (object)facilityId);
                cmd.Parameters.AddWithValue("@name", (object)(requesterName ?? "").Trim());
            }).ConfigureAwait(false);
        int id = idTable.Rows.Count > 0 ? Convert.ToInt32(idTable.Rows[0]["booking_id"]) : 0;
        AuditTrailService.Log("FacilityBooking", "facility_booking", id, "CREATE", null,
            new { facilityId, requesterName, startAt, endAt }, "Booking created.");
        return id;
    }

    public async Task UpdateBookingAsync(int bookingId, int facilityId, string requesterName,
        string? contact, string purpose, DateTime startAt, DateTime endAt,
        int? expectedGuests, decimal totalAmount)
    {
        if (startAt >= endAt)
            throw new InvalidOperationException("End time must be later than start time.");

        if (await HasConflictAsync(facilityId, startAt, endAt, bookingId).ConfigureAwait(false))
            throw new InvalidOperationException("This facility is already booked for the selected time range.");

        const string sql = @"UPDATE facility_booking
                                SET facility_id = @fid,
                                    requester_name = @name,
                                    requester_contact = @contact,
                                    purpose = @purpose,
                                    start_at = @start,
                                    end_at = @end,
                                    expected_guests = @guests,
                                    total_amount = @amount
                              WHERE booking_id = @id";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@id", (object)bookingId);
            cmd.Parameters.AddWithValue("@fid", (object)facilityId);
            cmd.Parameters.AddWithValue("@name", (object)(requesterName ?? "").Trim());
            cmd.Parameters.AddWithValue("@contact", DbNullIfEmpty(contact));
            cmd.Parameters.AddWithValue("@purpose", (object)(purpose ?? "").Trim());
            cmd.Parameters.AddWithValue("@start", (object)startAt);
            cmd.Parameters.AddWithValue("@end", (object)endAt);
            cmd.Parameters.AddWithValue("@guests", expectedGuests.HasValue ? (object)expectedGuests.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@amount", (object)totalAmount);
        }).ConfigureAwait(false);
        AuditTrailService.Log("FacilityBooking", "facility_booking", bookingId, "UPDATE",
            null, new { facilityId, requesterName }, "Booking updated.");
    }

    public async Task<DataTable> GetBookingAsync(int bookingId)
    {
        return await DatabaseManagerAsync.LoadTableAsync(
            "SELECT * FROM facility_booking WHERE booking_id = @id LIMIT 1",
            cmd => cmd.Parameters.AddWithValue("@id", (object)bookingId)).ConfigureAwait(false);
    }

    public async Task UpdateBookingStatusAsync(int bookingId, string status, string? reason = null)
    {
        string normalized = NormalizeBookingStatus(status);
        const string sql = @"UPDATE facility_booking
                                SET status = @status,
                                    cancellation_reason = CASE WHEN @status IN ('REJECTED','CANCELLED') THEN @reason ELSE cancellation_reason END,
                                    approved_at = CASE WHEN @status = 'APPROVED' THEN CURRENT_TIMESTAMP ELSE approved_at END
                              WHERE booking_id = @id";
        await DatabaseManagerAsync.ExecuteNonQueryAsync(sql, cmd =>
        {
            cmd.Parameters.AddWithValue("@id", (object)bookingId);
            cmd.Parameters.AddWithValue("@status", (object)normalized);
            cmd.Parameters.AddWithValue("@reason", DbNullIfEmpty(reason));
        }).ConfigureAwait(false);
        AuditTrailService.Log("FacilityBooking", "facility_booking", bookingId, "STATUS_CHANGE",
            null, new { status = normalized, reason }, "Booking status updated.");
    }

    public async Task UpdatePaymentStatusAsync(int bookingId, string paymentStatus)
    {
        string normalized = NormalizePaymentStatus(paymentStatus);
        await DatabaseManagerAsync.ExecuteNonQueryAsync(
            "UPDATE facility_booking SET payment_status = @status WHERE booking_id = @id",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@id", (object)bookingId);
                cmd.Parameters.AddWithValue("@status", (object)normalized);
            }).ConfigureAwait(false);
    }

    public async Task DeleteBookingAsync(int bookingId)
    {
        await DatabaseManagerAsync.ExecuteNonQueryAsync(
            "DELETE FROM facility_booking WHERE booking_id = @id",
            cmd => cmd.Parameters.AddWithValue("@id", (object)bookingId)).ConfigureAwait(false);
        AuditTrailService.Log("FacilityBooking", "facility_booking", bookingId, "DELETE", null, null, "Booking deleted.");
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static string NormalizeFacilityType(string? type)
    {
        string t = (type ?? "").Trim().ToUpperInvariant();
        return Array.IndexOf(FacilityTypes, t) >= 0 ? t : "VENUE";
    }

    private static string NormalizeBookingStatus(string? status)
    {
        string s = (status ?? "").Trim().ToUpperInvariant();
        return Array.IndexOf(BookingStatuses, s) >= 0 ? s : "PENDING";
    }

    private static string NormalizePaymentStatus(string? status)
    {
        string s = (status ?? "").Trim().ToUpperInvariant();
        return Array.IndexOf(PaymentStatuses, s) >= 0 ? s : "UNPAID";
    }

    private static object DbNullIfEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
        return value.Trim();
    }
}
