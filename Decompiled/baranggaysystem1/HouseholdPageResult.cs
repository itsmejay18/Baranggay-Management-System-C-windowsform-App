using System;
using System.Collections.Generic;

namespace baranggaysystem1;

internal sealed class HouseholdPageResult
{
	public IReadOnlyList<HouseholdListItem> Items { get; set; } = Array.Empty<HouseholdListItem>();

	public int TotalRows { get; set; }

	public int PageNumber { get; set; }

	public int PageSize { get; set; }

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
