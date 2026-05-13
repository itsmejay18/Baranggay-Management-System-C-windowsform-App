using System;
using System.Collections.Generic;

namespace baranggaysystem1;

internal sealed class HouseholdPageResult
{
	public IReadOnlyList<HouseholdListItem> Items { get; init; } = Array.Empty<HouseholdListItem>();

	public int TotalRows { get; init; }

	public int PageNumber { get; init; }

	public int PageSize { get; init; }

	public int TotalPages
	{
		get
		{
			if (PageSize > 0)
			{
				return (int)Math.Ceiling((double)TotalRows / (double)PageSize);
			}
			return 0;
		}
	}
}
