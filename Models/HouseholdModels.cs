using System;
using System.Collections.Generic;

namespace baranggaysystem1;

public sealed class HouseholdListItem
{
    public int HouseholdId { get; set; }
    public string HouseNo { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Subdivision { get; set; } = string.Empty;
    public string PurokName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int SeniorCount { get; set; }
    public int PwdCount { get; set; }
    public int FourPsCount { get; set; }
    public int VoterCount { get; set; }
    public int ActiveCaseCount { get; set; }
}

public sealed class HouseholdDetailsDto
{
    public int HouseholdId { get; set; }
    public int? PurokId { get; set; }
    public string HouseNo { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Subdivision { get; set; } = string.Empty;
    public string PurokName { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int MemberCount { get; set; }
    public int SeniorCount { get; set; }
    public int PwdCount { get; set; }
    public int FourPsCount { get; set; }
    public int VoterCount { get; set; }
    public int ActiveCaseCount { get; set; }
}

public sealed class HouseholdListFilters
{
    public string? Search { get; set; }
    public string? SearchText { get => Search; set => Search = value; }
    public int BarangayId { get; set; }
    public int? PurokId { get; set; }
    public bool? WithSeniors { get; set; }
    public bool? WithPwd { get; set; }
    public bool? With4Ps { get; set; }
    public bool? EmptyOnly { get; set; }
    public bool? EmptyHouseholdOnly { get => EmptyOnly; set => EmptyOnly = value; }
    public bool? ActiveCases { get; set; }
    public bool? HasActiveCasesOnly { get => ActiveCases; set => ActiveCases = value; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class HouseholdPageResult
{
    public IReadOnlyList<HouseholdListItem> Items { get; set; } = Array.Empty<HouseholdListItem>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

    // Alias for backward compatibility
    public int TotalRows => TotalCount;
    public int PageNumber => Page;
}

public sealed class HouseholdEditRecord
{
    public int HouseholdId { get; set; }
    public int? PurokId { get; set; }
    public string HouseNo { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Subdivision { get; set; } = string.Empty;
    public string? AddressNote { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public sealed class HouseholdSaveRequest
{
    public int? HouseholdId { get; set; }
    public int? PurokId { get; set; }
    public string HouseNo { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Subdivision { get; set; } = string.Empty;
    public string? AddressNote { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int BarangayId { get; set; }
}

public sealed class HouseholdMemberRecord
{
    public int ResidentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Relationship { get; set; }
    public bool IsHead { get; set; }
    public string? Status { get; set; }
    public bool HasPhoto { get; set; }
    public int? Age { get; set; }
    public string? Sex { get; set; }
    public string? CivilStatus { get; set; }
    public string? ContactNo { get; set; }
}

public sealed class HouseholdTransferHistoryItem
{
    public int ResidentId { get; set; }
    public string ResidentName { get; set; } = string.Empty;
    public int? FromHouseholdId { get; set; }
    public int? ToHouseholdId { get; set; }
    public string? OldAddress { get; set; }
    public string? NewAddress { get; set; }
    public string? Reason { get; set; }
    public DateTime? TransferredAt { get; set; }
    public string? TransferredBy { get; set; }
}

public sealed class ResidentPickerItem
{
    public int ResidentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Purok { get; set; }
    public string? Status { get; set; }
    public string? ContactNo { get; set; }
    public string? CurrentAddress { get; set; }
}
