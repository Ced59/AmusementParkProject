using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RankingEvidenceResult(
    RankingEvidenceLevel Level,
    bool IsEligibleForMainRanking,
    long UniqueContributorCount,
    long RatingObservationCount,
    long? DirectParkContributorCount,
    long? ItemContributorCount,
    int? EligibleItemCount,
    int? EligibleCategoryCount,
    RatingMethodologyVersion MethodologyVersion,
    RankingIneligibilityReason? IneligibilityReason,
    int? NextThreshold);
