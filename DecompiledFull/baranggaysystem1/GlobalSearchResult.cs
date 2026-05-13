namespace baranggaysystem1;

internal sealed record GlobalSearchResult(GlobalSearchEntityType EntityType, int Id, string Title, string Subtitle, int? ResidentId = null);
