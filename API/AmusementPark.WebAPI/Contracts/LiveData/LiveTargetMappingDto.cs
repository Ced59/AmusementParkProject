namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class LiveTargetMappingDto
{
    public Guid MappingId { get; init; }

    public string Version { get; init; } = string.Empty;

    public string SourceId { get; init; } = string.Empty;

    public LiveExternalTargetDto ExternalTarget { get; init; } = new();

    public LiveInternalTargetDto? Target { get; init; }

    public LiveMappingStatusDto Status { get; init; }

    public LiveMappingConfidenceDto Confidence { get; init; }

    public DateTime ValidFromUtc { get; init; }

    public DateTime? ValidToUtc { get; init; }

    public int Revision { get; init; }

    public int? SupersedesRevision { get; init; }

    public string? ReviewedByUserId { get; init; }

    public string? ReviewNote { get; init; }

    public DateTime RecordedAtUtc { get; init; }

    public bool IsEligibleForLiveUse { get; init; }
}
