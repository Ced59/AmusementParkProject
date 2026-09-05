using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingCategoryCoverageResult(
    string ScopeKey,
    ParkItemCategory Category,
    int CandidateCount,
    int EligibleCount,
    bool HasMinimumComparableEntries);
