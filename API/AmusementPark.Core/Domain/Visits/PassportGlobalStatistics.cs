namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportGlobalStatistics(
    long ParkCount,
    PassportStatisticsSummary Summary,
    IReadOnlyCollection<PassportGlobalYearActivity> ActivityByYear,
    IReadOnlyCollection<PassportGlobalParkActivity> TopParks,
    IReadOnlyCollection<PassportGlobalItemActivity> TopItems,
    IReadOnlyCollection<PassportGlobalRatingEvolution> RatingEvolution);
