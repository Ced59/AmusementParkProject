namespace AmusementPark.WebAPI.Contracts.History;

public sealed record PublicHistoricalSubjectComparisonDto
{
    public string ComparisonKey { get; init; } = string.Empty;

    public string SubjectType { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string NameOrigin { get; init; } = string.Empty;

    public string? PreviousName { get; init; }

    public string? NextName { get; init; }

    public string PresenceChange { get; init; } = string.Empty;

    public bool IsRenamed { get; init; }

    public bool IsMoved { get; init; }

    public string? PreviousZoneName { get; init; }

    public string? NextZoneName { get; init; }

    public string? PreviousCategory { get; init; }

    public string? NextCategory { get; init; }

    public string FromOperationalState { get; init; } = string.Empty;

    public string ToOperationalState { get; init; } = string.Empty;

    public int FromSupportingSourceCount { get; init; }

    public int ToSupportingSourceCount { get; init; }
}
