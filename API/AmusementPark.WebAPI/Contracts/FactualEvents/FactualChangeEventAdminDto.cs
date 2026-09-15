namespace AmusementPark.WebAPI.Contracts.FactualEvents;

public sealed class FactualChangeEventAdminDto
{
    public string EventId { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public int DefinitionVersion { get; init; }

    public FactualChangeTargetAdminDto Target { get; init; } = new FactualChangeTargetAdminDto();

    public FactualFactValueAdminDto? PreviousValue { get; init; }

    public FactualFactValueAdminDto? NewValue { get; init; }

    public FactualSourceReferenceAdminDto Source { get; init; } = new FactualSourceReferenceAdminDto();

    public string Confidence { get; init; } = string.Empty;

    public DateTime OccurredAtUtc { get; init; }

    public long Revision { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public DateTime? VerifiedAtUtc { get; init; }

    public DateTime? PublishedAtUtc { get; init; }

    public long Version { get; init; }

    public bool CanBeDistributed { get; init; }
}
