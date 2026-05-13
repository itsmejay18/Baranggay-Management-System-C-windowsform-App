namespace baranggaysystem1;

public sealed class LookupItem
{
	public int Id { get; }

	public string Name { get; }

	public LookupItem(int id, string name)
	{
		Id = id;
		Name = name;
	}

	public override string ToString()
	{
		return Name;
	}
}
