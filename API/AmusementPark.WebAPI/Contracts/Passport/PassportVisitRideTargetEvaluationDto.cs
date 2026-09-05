namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportVisitRideTargetEvaluationDto
{
    public string ParkItemId { get; init; } = string.Empty;

    public PassportHistoricalConsistencyDto HistoricalConsistency { get; init; }

    public DateOnly? OpeningDate { get; init; }

    public DateOnly? ClosingDate { get; init; }
}
