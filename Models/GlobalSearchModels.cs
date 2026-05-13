namespace baranggaysystem1;

public enum GlobalSearchScope
{
    All,
    Residents,
    Certificates,
    Blotter,
    Users
}

public enum GlobalSearchEntityType
{
    Resident,
    Certificate,
    Blotter,
    User
}

public sealed class GlobalSearchResult
{
    public int Id { get; set; }
    public GlobalSearchEntityType EntityType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public int? ResidentId { get; set; }
}
