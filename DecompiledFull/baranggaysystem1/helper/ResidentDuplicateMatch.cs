using System;

namespace baranggaysystem1.helper;

internal sealed class ResidentDuplicateMatch
{
	internal int ResidentId { get; init; }

	internal string FullName { get; init; } = string.Empty;

	internal DateTime BirthDate { get; init; }

	internal string AddressLabel { get; init; } = string.Empty;
}
