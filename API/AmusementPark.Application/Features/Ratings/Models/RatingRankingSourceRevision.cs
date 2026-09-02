using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingRankingSourceRevision(
    RankingScopeKey ScopeKey,
    long Revision,
    DateTime UpdatedAtUtc,
    int PendingMutationCount = 0,
    DateTime? MutationLeaseExpiresAtUtc = null,
    RatingMethodologyVersion? UnavailableMethodologyVersion = null,
    long? HighestUnavailableSourceRevision = null,
    string? UnavailableReasonCode = null,
    IReadOnlyCollection<RatingRankingRecoveredMutation>? RecoveredMutations = null,
    RatingMethodologyVersion? CacheConvergedMethodologyVersion = null,
    long? HighestCacheConvergedSourceRevision = null)
{
    public bool IsRebuildable => this.PendingMutationCount == 0
        && (this.RecoveredMutations?.Count ?? 0) == 0;

    public bool CoversUnavailable(
        RatingMethodologyVersion methodologyVersion,
        long sourceRevision)
    {
        return this.UnavailableMethodologyVersion == methodologyVersion
            && this.HighestUnavailableSourceRevision >= sourceRevision;
    }

    public bool CoversCacheConvergence(
        RatingMethodologyVersion methodologyVersion,
        long sourceRevision)
    {
        return this.CacheConvergedMethodologyVersion == methodologyVersion
            && this.HighestCacheConvergedSourceRevision >= sourceRevision;
    }
}
