namespace AmusementPark.Core.Domain.Sharing;

public sealed record ProfileComparisonPassportData(
    string? DisplayName,
    IReadOnlyCollection<ProfileComparisonParkData> Parks,
    IReadOnlyCollection<ProfileComparisonRatingData> Ratings,
    IReadOnlyCollection<ProfileComparisonYearData> Years,
    IReadOnlyCollection<ProfileComparisonMissedItemData> MissedItems,
    bool HasIncompleteCatalog);
