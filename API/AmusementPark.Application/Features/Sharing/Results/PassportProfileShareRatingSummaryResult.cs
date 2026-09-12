namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PassportProfileShareRatingSummaryResult(
    long RatedCount,
    long EligibleCount,
    double? Average);
