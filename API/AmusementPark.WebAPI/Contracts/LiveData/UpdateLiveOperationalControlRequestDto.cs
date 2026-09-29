namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class UpdateLiveOperationalControlRequestDto
{
    public LiveOperationalScopeTypeDto ScopeType { get; set; }

    public string SourceId { get; set; } = string.Empty;

    public string? ExternalEntityId { get; set; }

    public string? InternalParkId { get; set; }

    public LiveTargetTypeDto? TargetType { get; set; }

    public string? InternalTargetId { get; set; }

    public bool CollectionEnabled { get; set; }

    public bool PublicReadEnabled { get; set; }

    public int ExpectedRevision { get; set; }

    public string Reason { get; set; } = string.Empty;
}
