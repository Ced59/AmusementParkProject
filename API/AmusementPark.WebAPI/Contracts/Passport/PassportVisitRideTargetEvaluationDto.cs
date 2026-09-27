namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportVisitRideTargetEvaluationDto
{
    public string ParkItemId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string OperationalState { get; init; } = string.Empty;

    public PassportHistoricalConsistencyDto HistoricalConsistency { get; init; }

    public bool IsHistoricalOnly { get; init; }

    public string? MainImageId { get; init; }

    public string? ZoneId { get; init; }

    public string? LifecycleStatus { get; init; }

    public DateOnly? OpeningDate { get; init; }

    public DateOnly? ClosingDate { get; init; }
}
