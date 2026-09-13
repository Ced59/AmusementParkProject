namespace AmusementPark.Application.Features.ParkGraphUpserts.Results;

public sealed class ParkGraphUpsertChange
{
    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? EntityKey { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string ChangeType { get; set; } = "Unchanged";

    public string MatchedBy { get; set; } = "none";

    public List<ParkGraphUpsertFieldChange> Fields { get; set; } = new List<ParkGraphUpsertFieldChange>();
}
