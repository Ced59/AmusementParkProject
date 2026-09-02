using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RatingRankingRebuildScopeJobHandler : IDurableBackgroundJobHandler
{
    private static readonly RankingSnapshotId ChecksumSnapshotId = RankingSnapshotId.Parse("checksum");
    private readonly IRankingScopeRegistry scopeRegistry;
    private readonly IRatingRankingSourceRevisionRepository sourceRevisionRepository;
    private readonly IRankingSnapshotRepository snapshotRepository;
    private readonly IRatingRankingSnapshotBuilder snapshotBuilder;
    private readonly RankingSnapshotChecksumCalculator checksumCalculator;
    private readonly IRatingRankingPublicationCacheInvalidator publicationCacheInvalidator;

    public RatingRankingRebuildScopeJobHandler(
        IRankingScopeRegistry scopeRegistry,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository,
        IRankingSnapshotRepository snapshotRepository,
        IRatingRankingSnapshotBuilder snapshotBuilder,
        RankingSnapshotChecksumCalculator checksumCalculator,
        IRatingRankingPublicationCacheInvalidator publicationCacheInvalidator)
    {
        this.scopeRegistry = scopeRegistry;
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.snapshotRepository = snapshotRepository;
        this.snapshotBuilder = snapshotBuilder;
        this.checksumCalculator = checksumCalculator;
        this.publicationCacheInvalidator = publicationCacheInvalidator;
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; } =
        new DurableBackgroundJobHandlerDefinition(
            RatingRankingRebuildScopeJob.Kind,
            DurableBackgroundJobWorkload.Heavy,
            new[] { RatingRankingRebuildScopeJob.PayloadVersion },
            TimeSpan.FromMinutes(10),
            maximumAttempts: 5,
            initialRetryDelay: TimeSpan.FromMinutes(1),
            maximumRetryDelay: TimeSpan.FromMinutes(15),
            maximumConcurrency: 1);

    public async Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (!TryResolveRequest(
                context,
                this.scopeRegistry,
                out RankingScopeDefinition? scope,
                out long requestedRevision,
                out bool forceRebuild))
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                RatingRankingRebuildErrorCodes.InvalidPayload);
        }

        RankingPublicationPointer? pointer = await this.snapshotRepository.GetPointerAsync(
            scope.Key,
            cancellationToken);
        if (!forceRebuild && IsCovered(pointer, scope, requestedRevision))
        {
            return await this.CompleteWithCacheInvalidationAsync(
                scope,
                requestedRevision,
                cancellationToken);
        }

        RevisionFenceCheck initialFence = await this.CheckRevisionFenceAsync(
            scope,
            requestedRevision,
            expectedGlobalRevision: null,
            cancellationToken);
        if (initialFence.Disposition == RevisionFenceDisposition.NewerRevisionExists)
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        if (RequiresRetry(initialFence.Disposition))
        {
            return DurableBackgroundJobHandlerResult.Retry(
                RatingRankingRebuildErrorCodes.SourceRevisionUnavailable);
        }

        long? expectedGlobalRevision = initialFence.GlobalRevision;

        RatingRankingSnapshotBuildPlan plan = await this.snapshotBuilder.BuildAsync(
            scope,
            cancellationToken);
        if (plan.IsSourceTruncated)
        {
            RevisionFenceCheck overflowFence = await this.CheckRevisionFenceAsync(
                scope,
                requestedRevision,
                expectedGlobalRevision,
                cancellationToken);
            if (overflowFence.Disposition == RevisionFenceDisposition.NewerRevisionExists)
            {
                return DurableBackgroundJobHandlerResult.Success();
            }

            if (RequiresRetry(overflowFence.Disposition))
            {
                return DurableBackgroundJobHandlerResult.Retry(
                    RatingRankingRebuildErrorCodes.SourceRevisionUnavailable);
            }

            await this.sourceRevisionRepository.MarkUnavailableAsync(
                scope.Key,
                scope.MethodologyVersion,
                requestedRevision,
                RatingRankingRebuildErrorCodes.SourceSetTruncated,
                cancellationToken);
            return DurableBackgroundJobHandlerResult.DeadLetter(
                RatingRankingRebuildErrorCodes.SourceSetTruncated);
        }

        RevisionFenceCheck preWriteFence = await this.CheckRevisionFenceAsync(
            scope,
            requestedRevision,
            expectedGlobalRevision,
            cancellationToken);
        if (preWriteFence.Disposition == RevisionFenceDisposition.NewerRevisionExists)
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        if (RequiresRetry(preWriteFence.Disposition))
        {
            return DurableBackgroundJobHandlerResult.Retry(
                RatingRankingRebuildErrorCodes.SourceRevisionUnavailable);
        }

        if (!scope.EvaluatePublication(plan.EligibleEntries.Count).IsEligible)
        {
            RankingSnapshotRetirementResult retirement =
                await this.snapshotRepository.RetirePublicationAsync(
                    new RetireRankingPublicationRequest(
                        scope.Key,
                        scope.MethodologyVersion,
                        requestedRevision),
                    cancellationToken);
            if (retirement.Disposition is RankingSnapshotRetirementDisposition.Retired
                or RankingSnapshotRetirementDisposition.AlreadyUnavailable)
            {
                await this.sourceRevisionRepository.MarkUnavailableAsync(
                    scope.Key,
                    scope.MethodologyVersion,
                    requestedRevision,
                    RatingRankingRebuildErrorCodes.BelowMinimumEligibleEntries,
                    cancellationToken);
                return await this.CompleteWithCacheInvalidationAsync(
                    scope,
                    requestedRevision,
                    cancellationToken);
            }

            return retirement.Disposition == RankingSnapshotRetirementDisposition.Stale
                ? DurableBackgroundJobHandlerResult.Success()
                : DurableBackgroundJobHandlerResult.Retry(
                    RatingRankingRebuildErrorCodes.RetirementConflict);
        }

        IReadOnlyCollection<RankingSnapshotChunk> checksumChunks = this.CreateChunks(
            plan.EligibleEntries,
            scope.PageSize,
            ChecksumSnapshotId,
            buildAttempt: 1);
        RankingSnapshotChecksum checksum = this.checksumCalculator.CalculateSnapshot(
            plan.TotalEntryCount,
            plan.EligibleEntries.Count,
            scope.PageSize,
            checksumChunks);
        RankingSnapshotBuildStartResult start = await this.snapshotRepository.StartBuildAsync(
            new StartRankingSnapshotBuildRequest(
                scope.Key,
                scope.MethodologyVersion,
                requestedRevision,
                plan.TotalEntryCount,
                plan.EligibleEntries.Count,
                checksum,
                forceRebuild),
            cancellationToken);
        if (start.Disposition == RankingSnapshotBuildStartDisposition.Conflict || start.Header is null)
        {
            return DurableBackgroundJobHandlerResult.Retry(
                RatingRankingRebuildErrorCodes.BuildConflict);
        }

        RankingSnapshotHeader header = start.Header;
        if (header.Status == RankingSnapshotStatus.Building)
        {
            IReadOnlyCollection<RankingSnapshotChunk> chunks = this.CreateChunks(
                plan.EligibleEntries,
                scope.PageSize,
                header.Id,
                header.BuildAttempt);
            foreach (RankingSnapshotChunk chunk in chunks)
            {
                RankingSnapshotChunkWriteResult write = await this.snapshotRepository.WriteChunkAsync(
                    chunk,
                    cancellationToken);
                if (write.Disposition is RankingSnapshotChunkWriteDisposition.Conflict
                    or RankingSnapshotChunkWriteDisposition.BuildNotWritable)
                {
                    await this.snapshotRepository.FailBuildAsync(
                        header.Id,
                        header.BuildAttempt,
                        RatingRankingRebuildErrorCodes.ChunkWriteConflict,
                        cancellationToken);
                    return DurableBackgroundJobHandlerResult.Retry(
                        RatingRankingRebuildErrorCodes.ChunkWriteConflict);
                }
            }

            RankingSnapshotValidationResult validation = await this.snapshotRepository.ValidateBuildAsync(
                header.Id,
                header.BuildAttempt,
                cancellationToken);
            if (validation.Disposition is not (RankingSnapshotValidationDisposition.Validated
                or RankingSnapshotValidationDisposition.AlreadyValidated))
            {
                return DurableBackgroundJobHandlerResult.Retry(
                    validation.ErrorCode ?? RatingRankingRebuildErrorCodes.ValidationFailed);
            }

            header = validation.Header ?? header;
        }

        if (header.Status is not (RankingSnapshotStatus.Validated
            or RankingSnapshotStatus.Current
            or RankingSnapshotStatus.Superseded))
        {
            return DurableBackgroundJobHandlerResult.Retry(
                RatingRankingRebuildErrorCodes.ValidationFailed);
        }

        RevisionFenceCheck publicationFence = await this.CheckRevisionFenceAsync(
            scope,
            requestedRevision,
            expectedGlobalRevision,
            cancellationToken);
        if (publicationFence.Disposition == RevisionFenceDisposition.NewerRevisionExists)
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        if (RequiresRetry(publicationFence.Disposition))
        {
            return DurableBackgroundJobHandlerResult.Retry(
                RatingRankingRebuildErrorCodes.SourceRevisionUnavailable);
        }

        RankingSnapshotPublicationResult publication = await this.snapshotRepository.PublishAsync(
            header.Id,
            cancellationToken);
        if (publication.Disposition is RankingSnapshotPublicationDisposition.Published
            or RankingSnapshotPublicationDisposition.AlreadyPublished
            or RankingSnapshotPublicationDisposition.Stale)
        {
            return await this.CompleteWithCacheInvalidationAsync(
                scope,
                requestedRevision,
                cancellationToken);
        }

        return DurableBackgroundJobHandlerResult.Retry(
            RatingRankingRebuildErrorCodes.PublicationConflict);
    }

    private async Task<DurableBackgroundJobHandlerResult> CompleteWithCacheInvalidationAsync(
        RankingScopeDefinition scope,
        long requestedRevision,
        CancellationToken cancellationToken)
    {
        try
        {
            bool invalidated = await this.publicationCacheInvalidator.InvalidateAsync(cancellationToken);
            if (!invalidated)
            {
                return DurableBackgroundJobHandlerResult.Retry(
                    RatingRankingRebuildErrorCodes.CacheInvalidationFailed);
            }

            await this.sourceRevisionRepository.MarkCacheConvergedAsync(
                scope.Key,
                scope.MethodologyVersion,
                requestedRevision,
                cancellationToken);
            return DurableBackgroundJobHandlerResult.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return DurableBackgroundJobHandlerResult.Retry(
                RatingRankingRebuildErrorCodes.CacheInvalidationFailed);
        }
    }

    private IReadOnlyCollection<RankingSnapshotChunk> CreateChunks(
        IReadOnlyCollection<RankingSnapshotEntry> entries,
        int chunkSize,
        RankingSnapshotId snapshotId,
        int buildAttempt)
    {
        List<RankingSnapshotEntry> orderedEntries = entries
            .OrderBy(static entry => entry.Position)
            .ToList();
        List<RankingSnapshotChunk> chunks = new List<RankingSnapshotChunk>();
        for (int offset = 0; offset < orderedEntries.Count; offset += chunkSize)
        {
            IReadOnlyCollection<RankingSnapshotEntry> chunkEntries = orderedEntries
                .Skip(offset)
                .Take(chunkSize)
                .ToArray();
            RankingSnapshotChecksum checksum = this.checksumCalculator.CalculateChunk(chunkEntries);
            chunks.Add(new RankingSnapshotChunk(
                snapshotId,
                chunks.Count,
                chunkEntries,
                checksum,
                buildAttempt));
        }

        return chunks;
    }

    private async Task<RevisionFenceCheck> CheckRevisionFenceAsync(
        RankingScopeDefinition scope,
        long requestedRevision,
        long? expectedGlobalRevision,
        CancellationToken cancellationToken)
    {
        RatingRankingSourceRevision? current = await this.sourceRevisionRepository.GetAsync(
            scope.Key,
            cancellationToken);
        if (current is not null && !current.IsRebuildable)
        {
            return new RevisionFenceCheck(RevisionFenceDisposition.MutationPending, null);
        }

        long currentRevision = current?.Revision ?? 0;
        if (currentRevision > requestedRevision)
        {
            return new RevisionFenceCheck(RevisionFenceDisposition.NewerRevisionExists, null);
        }

        if (currentRevision < requestedRevision)
        {
            return new RevisionFenceCheck(
                RevisionFenceDisposition.RequestedRevisionUnavailable,
                null);
        }

        if (scope.TargetFamily != RankingTargetFamily.ParkItems)
        {
            return new RevisionFenceCheck(RevisionFenceDisposition.Current, null);
        }

        RatingRankingSourceRevision? globalRevision =
            await this.sourceRevisionRepository.GetAsync(
                CanonicalRankingScopes.GlobalParks.Key,
                cancellationToken);
        if (globalRevision is not null && !globalRevision.IsRebuildable)
        {
            return new RevisionFenceCheck(RevisionFenceDisposition.MutationPending, null);
        }

        long currentGlobalRevision = globalRevision?.Revision ?? 0;
        if (expectedGlobalRevision.HasValue
            && currentGlobalRevision != expectedGlobalRevision.Value)
        {
            return new RevisionFenceCheck(
                RevisionFenceDisposition.DependencyChanged,
                currentGlobalRevision);
        }

        return new RevisionFenceCheck(
            RevisionFenceDisposition.Current,
            currentGlobalRevision);
    }

    private static bool RequiresRetry(RevisionFenceDisposition disposition)
    {
        return disposition is RevisionFenceDisposition.RequestedRevisionUnavailable
            or RevisionFenceDisposition.MutationPending
            or RevisionFenceDisposition.DependencyChanged;
    }

    private static bool TryResolveRequest(
        DurableBackgroundJobExecutionContext context,
        IRankingScopeRegistry scopeRegistry,
        [NotNullWhen(true)] out RankingScopeDefinition? scope,
        out long requestedRevision,
        out bool forceRebuild)
    {
        scope = null;
        requestedRevision = 0;
        forceRebuild = false;
        if (context.PayloadVersion != RatingRankingRebuildScopeJob.PayloadVersion)
        {
            return false;
        }

        RatingRankingRebuildScopePayload? payload;
        try
        {
            payload = context.Payload.Deserialize<RatingRankingRebuildScopePayload>();
        }
        catch (JsonException)
        {
            return false;
        }

        if (payload is null
            || context.RequestedRevision is not long contextRevision
            || contextRevision < 0
            || payload.RequestedSourceRevision != contextRevision)
        {
            return false;
        }

        RatingMethodologyVersion methodologyVersion;
        try
        {
            methodologyVersion = RatingMethodologyVersion.Parse(payload.MethodologyVersion);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (!scopeRegistry.TryResolve(payload.ScopeKey, methodologyVersion, out scope))
        {
            return false;
        }

        requestedRevision = contextRevision;
        forceRebuild = payload.ForceRebuild;
        return true;
    }

    private static bool IsCovered(
        RankingPublicationPointer? pointer,
        RankingScopeDefinition scope,
        long requestedRevision)
    {
        return pointer is not null
            && pointer.MethodologyVersion == scope.MethodologyVersion
            && pointer.HighestPublishedSourceRevision >= requestedRevision;
    }

    private enum RevisionFenceDisposition
    {
        Current,
        NewerRevisionExists,
        RequestedRevisionUnavailable,
        MutationPending,
        DependencyChanged,
    }

    private sealed record RevisionFenceCheck(
        RevisionFenceDisposition Disposition,
        long? GlobalRevision);
}
