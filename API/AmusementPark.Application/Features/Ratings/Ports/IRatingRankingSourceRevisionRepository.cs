using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankingSourceRevisionRepository
{
    Task<RatingRankingMutationLease> BeginMutationAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken);

    Task<RatingRankingMutationLease> BeginMutationAsync(
        RankingScopeKey scopeKey,
        RatingRankingMutationRecoveryTarget recoveryTarget,
        CancellationToken cancellationToken);

    Task<RatingRankingSourceRevision> CompleteMutationAsync(
        RatingRankingMutationLease mutationLease,
        bool sourceChanged,
        CancellationToken cancellationToken);

    Task MarkUnavailableAsync(
        RankingScopeKey scopeKey,
        RatingMethodologyVersion methodologyVersion,
        long sourceRevision,
        string reasonCode,
        CancellationToken cancellationToken);

    Task<RatingRankingSourceRevision?> GetAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken);

    Task<bool> AcknowledgeRecoveredParkItemTargetAsync(
        RankingScopeKey scopeKey,
        string targetId,
        CancellationToken cancellationToken);
}
