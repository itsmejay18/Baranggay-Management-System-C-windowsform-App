using System;

namespace baranggaysystem1.Models;

internal sealed class ResidentClassificationRecord
{
	public int ClassificationId { get; set; }

	public int BarangayId { get; set; }

	public string ClassificationType { get; set; } = "TAG";

	public string ClassificationKey { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public string ColorHex { get; set; } = "#3B82F6";

	public string Status { get; set; } = "ACTIVE";

	public bool IsSystem { get; set; }

	public int SortOrder { get; set; }

	public int UsageCount { get; set; }

	public string CreatedAtDisplay { get; set; } = string.Empty;

	public string TypeDisplay
	{
		get
		{
			if (!string.Equals(ClassificationType, "CATEGORY", StringComparison.OrdinalIgnoreCase))
			{
				return "Tag";
			}
			return "Category";
		}
	}

	public string StatusDisplay
	{
		get
		{
			if (!string.Equals(Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase))
			{
				return "Active";
			}
			return "Archived";
		}
	}

	public string SourceDisplay
	{
		get
		{
			if (!IsSystem)
			{
				return "Custom";
			}
			return "System";
		}
	}

	public string UsageDisplay
	{
		get
		{
			if (UsageCount != 1)
			{
				return $"{UsageCount:N0} residents";
			}
			return "1 resident";
		}
	}
}
