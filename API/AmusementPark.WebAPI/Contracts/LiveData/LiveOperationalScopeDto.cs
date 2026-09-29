namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class LiveOperationalScopeDto
{
    public LiveOperationalScopeTypeDto ScopeType { get; set; }

    public string SourceId { get; set; } = string.Empty;

    public string? ExternalEntityId { get; set; }

    public string? InternalParkId { get; set; }

    public LiveTargetTypeDto? TargetType { get; set; }

    public string? InternalTargetId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? ParentDisplayName { get; set; }

    public bool CollectionEnabled { get; set; }

    public bool PublicReadEnabled { get; set; }

    public bool EffectiveCollectionEnabled { get; set; }

    public bool EffectivePublicReadEnabled { get; set; }

    public int Revision { get; set; }

    public string? Reason { get; set; }

    public string? ChangedByUserId { get; set; }

    public DateTime? RecordedAtUtc { get; set; }
}
