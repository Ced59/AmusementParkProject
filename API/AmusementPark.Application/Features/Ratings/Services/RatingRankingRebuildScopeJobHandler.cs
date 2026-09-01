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

    public RatingRankingRebuildScopeJobHandler(
        IRankingScopeRegistry scopeRegistry,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository,
        IRankingSnapshotRepository snapshotRepository,
        IRatingRankingSnapshotBuilder snapshotBuilder,
        RankingSnapshotChecksumCalculator checksumCalculator)
    {
        this.scopeRegistry = scopeRegistry;
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.snapshotRepository = snapshotRepository;
        this.snapshotBuilder = snapshotBuilder;
        this.checksumCalculator = checksumCalculator;
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
                out long requestedRevision))
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                RatingRankingRebuildErrorCodes.InvalidPayload);
        }

        RankingPublicationPointer? pointer = await this.snapshotRepository.GetPointerAsync(
            scope.Key,
            cancellationToken);
        if (IsCovered(pointer, scope, requestedRevision))
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        RevisionFenceDisposition initialFence = await this.CheckRevisionFenceAsync(
            scope.Key,
            requestedRevision,
            cancellationToken);
        if (initialFence == RevisionFenceDisposition.NewerRevisionExists)
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        if (initialFence is RevisionFenceDisposition.RequestedRevisionUnavailable
            or RevisionFenceDisposition.MutationPending)
        {
            return DurableBackgroundJobHandlerResult.Retry(
                RatingRankingRebuildErrorCodes.SourceRevisionUnavailable);
        }

        RatingRankingSnapshotBuildPlan plan = await this.snapshotBuilder.BuildAsync(
            scope,
            cancellationToken);
        if (plan.IsSourceTruncated)
        {
            RevisionFenceDisposition overflowFence = await this.CheckRevisionFenceAsync(
                scope.Key,
                requestedRevision,
                cancellationToken);
            if (overflowFence == RevisionFenceDisposition.NewerRevisionExists)
            {
                return DurableBackgroundJobHandlerResult.Success();
            }

            if (overflowFence is RevisionFenceDisposition.RequestedRevisionUnavailable
                or RevisionFenceDisposition.MutationPending)
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

        RevisionFenceDisposition preWriteFence = await this.CheckRevisionFenceAsync(
            scope.Key,
            requestedRevision,
            cancellationToken);
        if (preWriteFence == RevisionFenceDisposition.NewerRevisionExists)
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        if (preWriteFence is RevisionFenceDisposition.RequestedRevisionUnavailable
            or RevisionFenceDisposition.MutationPending)
        {
            return DurableBackgroundJobHandlerResult.Retry(
                RatingRankingRebuildErrorCodes.SourceRevisionUnavailable);
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
                checksum),
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

        RevisionFenceDisposition publicationFence = await this.CheckRevisionFenceAsync(
            scope.Key,
            requestedRevision,
            cancellationToken);
        if (publicationFence == RevisionFenceDisposition.NewerRevisionExists)
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        if (publicationFence is RevisionFenceDisposition.RequestedRevisionUnavailable
            or RevisionFenceDisposition.MutationPending)
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
                return DurableBackgroundJobHandlerResult.Success();
            }

            return retirement.Disposition == RankingSnapshotRetirementDisposition.Stale
                ? DurableBackgroundJobHandlerResult.Success()
                : DurableBackgroundJobHandlerResult.Retry(
                    RatingRankingRebuildErrorCodes.RetirementConflict);
        }

        RankingSnapshotPublicationResult publication = await this.snapshotRepository.PublishAsync(
            header.Id,
            cancellationToken);
        return publication.Disposition switch
        {
            RankingSnapshotPublicationDisposition.Published => DurableBackgroundJobHandlerResult.Success(),
            RankingSnapshotPublicationDisposition.AlreadyPublished => DurableBackgroundJobHandlerResult.Success(),
            RankingSnapshotPublicationDisposition.Stale => DurableBackgroundJobHandlerResult.Success(),
            _ => DurableBackgroundJobHandlerResult.Retry(
                RatingRankingRebuildErrorCodes.PublicationConflict),
        };
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

    private async Task<RevisionFenceDisposition> CheckRevisionFenceAsync(
        RankingScopeKey scopeKey,
        long requestedRevision,
        CancellationToken cancellationToken)
    {
        RatingRankingSourceRevision? current = await this.sourceRevisionRepository.GetAsync(
            scopeKey,
            cancellationToken);
        if (current is not null && !current.IsRebuildable)
        {
            return RevisionFenceDisposition.MutationPending;
        }

        long currentRevision = current?.Revision ?? 0;
        if (currentRevision > requestedRevision)
        {
            return RevisionFenceDisposition.NewerRevisionExists;
        }

        return currentRevision < requestedRevision
            ? RevisionFenceDisposition.RequestedRevisionUnavailable
            : RevisionFenceDisposition.Current;
    }

    private static bool TryResolveRequest(
        DurableBackgroundJobExecutionContext context,
        IRankingScopeRegistry scopeRegistry,
        [NotNullWhen(true)] out RankingScopeDefinition? scope,
        out long requestedRevision)
    {
        scope = null;
        requestedRevision = 0;
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
    }
}
