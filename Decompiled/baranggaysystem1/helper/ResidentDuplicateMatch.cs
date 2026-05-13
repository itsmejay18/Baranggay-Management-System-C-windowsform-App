using System;

namespace baranggaysystem1.helper;

internal sealed class ResidentDuplicateMatch
{
	internal int ResidentId { get; set; }

	internal string FullName { get; set; } = string.Empty;

	internal DateTime BirthDate { get; set; }

	internal string AddressLabel { get; set; } = string.Empty;
}
