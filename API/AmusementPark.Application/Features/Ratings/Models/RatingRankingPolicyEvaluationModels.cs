using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingRankingPolicyCandidate(
    string Version,
    int ProvisionalMinUniqueContributors,
    int EligibleMinUniqueContributors,
    int EstablishedMinUniqueContributors,
    int StrongEvidenceMinUniqueContributors,
    int MinimumEligibleEntriesPerRanking,
    int MinimumEligibleItemsForParkItemComponent,
    int MinimumEligibleItemsPerCategory,
    int MinimumEligibleCategories,
    decimal ScoreTieEpsilon)
{
    public RankingEligibilityPolicy ToDomain()
    {
        return new RankingEligibilityPolicy(
            RatingMethodologyVersion.Parse(this.Version),
            this.ProvisionalMinUniqueContributors,
            this.EligibleMinUniqueContributors,
            this.EstablishedMinUniqueContributors,
            this.StrongEvidenceMinUniqueContributors,
            this.MinimumEligibleEntriesPerRanking,
            this.MinimumEligibleItemsForParkItemComponent,
            this.MinimumEligibleItemsPerCategory,
            this.MinimumEligibleCategories,
            this.ScoreTieEpsilon);
    }
}

public sealed record RatingRankingPolicyEvaluationEntry(
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    ParkItemCategory? ParkItemCategory,
    double Score,
    RankingEvidence? Evidence,
    ParkItemComponentEligibility? ParkItemComponent = null);

public sealed record RatingRankingPolicyEvaluationPlan(
    int TotalEntryCount,
    IReadOnlyCollection<RatingRankingPolicyEvaluationEntry> Entries,
    bool IsSourceTruncated);
