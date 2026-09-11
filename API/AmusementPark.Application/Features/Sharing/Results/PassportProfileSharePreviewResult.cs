using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PassportProfileSharePreviewResult(
    string? DisplayName,
    string? AvatarUrl,
    string? PublicCaption,
    ShareVisibility Visibility,
    bool AllowsComparisons,
    long? ParkCount,
    long? VisitCount,
    long? TotalRideCount,
    long? DistinctItemCount,
    PassportProfileShareRatingSummaryResult? VisitRatings,
    PassportProfileShareRatingSummaryResult? RideRatings,
    IReadOnlyCollection<PassportProfileShareCountryResult> Countries,
    IReadOnlyCollection<PassportProfileShareYearResult> Years,
    IReadOnlyCollection<PassportProfileShareParkResult> Parks,
    IReadOnlyCollection<PassportProfileShareRatingResult> PersonalRanking,
    IReadOnlyCollection<PassportProfileShareMissedItemResult> MissedItems,
    bool HasIncompleteCatalog,
    string CalculationVersion,
    bool IsEmpty);
