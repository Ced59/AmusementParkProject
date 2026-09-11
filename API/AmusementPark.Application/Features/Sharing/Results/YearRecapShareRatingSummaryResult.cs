namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record YearRecapShareRatingSummaryResult(
    long RatedCount,
    long EligibleCount,
    double? Average);
