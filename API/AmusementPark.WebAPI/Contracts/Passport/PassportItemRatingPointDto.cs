namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportItemRatingPointDto
{
    public string RideOccurrenceId { get; init; } = string.Empty;

    public string VisitId { get; init; } = string.Empty;

    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();

    public long SortPosition { get; init; }

    public double Rating { get; init; }
}
