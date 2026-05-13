namespace baranggaysystem1.helper;

internal sealed class PermissionCatalogItem
{
	public string Key { get; }

	public string GroupName { get; }

	public string Label { get; }

	public string Description { get; }

	public int GroupOrder { get; }

	public int ItemOrder { get; }

	public PermissionCatalogItem(string key, string groupName, string label, string description, int groupOrder, int itemOrder)
	{
		Key = key;
		GroupName = groupName;
		Label = label;
		Description = description;
		GroupOrder = groupOrder;
		ItemOrder = itemOrder;
	}
}
