namespace AmusementPark.Core.Domain.Sharing;

public sealed record ProfileComparisonCalculation(
    string? CreatorDisplayName,
    string? AcceptorDisplayName,
    IReadOnlyCollection<ProfileComparisonCategory> Categories,
    IReadOnlyCollection<ProfileComparisonParkResult> Parks,
    IReadOnlyCollection<ProfileComparisonRatingResult> Ratings,
    IReadOnlyCollection<ProfileComparisonYearResult> Years,
    IReadOnlyCollection<ProfileComparisonMissedItemResult> MissedItems,
    int CommonRatingCount,
    int MinimumRatingsForCorrelation,
    double? RatingCorrelation,
    bool HasIncompleteCatalog,
    string CalculationVersion);
