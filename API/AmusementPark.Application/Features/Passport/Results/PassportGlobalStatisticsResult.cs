namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportGlobalStatisticsResult(
    int? SelectedYear,
    string? SelectedParkId,
    IReadOnlyCollection<int> AvailableYears,
    IReadOnlyCollection<PassportGlobalFilterParkResult> AvailableParks,
    long ParkCount,
    PassportStatisticsSummaryResult Summary,
    IReadOnlyCollection<PassportGlobalYearActivityResult> ActivityByYear,
    IReadOnlyCollection<PassportGlobalParkActivityResult> TopParks,
    IReadOnlyCollection<PassportGlobalItemActivityResult> TopItems,
    IReadOnlyCollection<PassportGlobalRatingEvolutionResult> RatingEvolution);
