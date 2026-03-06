namespace baranggaysystem1;

internal sealed class LookupItem
{
    public LookupItem(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }

    public string Name { get; }

    public override string ToString()
    {
        return Name;
    }
}
