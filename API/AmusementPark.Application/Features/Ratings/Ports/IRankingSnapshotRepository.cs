using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRankingSnapshotRepository
{
    Task<RankingSnapshotBuildStartResult> StartBuildAsync(
        StartRankingSnapshotBuildRequest request,
        CancellationToken cancellationToken);

    Task<RankingSnapshotChunkWriteResult> WriteChunkAsync(
        RankingSnapshotChunk chunk,
        CancellationToken cancellationToken);

    Task<RankingSnapshotValidationResult> ValidateBuildAsync(
        RankingSnapshotId snapshotId,
        CancellationToken cancellationToken);

    Task<bool> FailBuildAsync(
        RankingSnapshotId snapshotId,
        string errorCode,
        CancellationToken cancellationToken);

    Task<RankingSnapshotPublicationResult> PublishAsync(
        RankingSnapshotId snapshotId,
        CancellationToken cancellationToken);

    Task<RankingSnapshotRollbackResult> RollbackAsync(
        RankingSnapshotRollbackRequest request,
        CancellationToken cancellationToken);

    Task<RankingPublicationPointer?> GetPointerAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken);

    Task<RankingSnapshotHeader?> GetCurrentHeaderAsync(
        RankingScopeKey scopeKey,
        RatingMethodologyVersion methodologyVersion,
        CancellationToken cancellationToken);

    Task<RankingSnapshotPage?> GetCurrentPageAsync(
        RankingScopeKey scopeKey,
        RatingMethodologyVersion methodologyVersion,
        int offset,
        int limit,
        CancellationToken cancellationToken);
}
