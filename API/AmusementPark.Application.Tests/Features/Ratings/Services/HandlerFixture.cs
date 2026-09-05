using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

internal sealed class HandlerFixture
{
    public HandlerFixture()
    {
        this.Scope = CanonicalRankingScopes.PublicItemCategories.Single(
            static scope => scope.Filter.ParkItemCategory == ParkItemCategory.Attraction);
        RankingScopeRegistry registry = new RankingScopeRegistry(
            "test-scopes",
            new[] { this.Scope });
        this.Revisions = new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        this.Snapshots = new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        this.Builder = new Mock<IRatingRankingSnapshotBuilder>(MockBehavior.Strict);
        this.CacheInvalidator = new RecordingPublicationCacheInvalidator();
        this.Handler = new RatingRankingRebuildScopeJobHandler(
            registry,
            this.Revisions.Object,
            this.Snapshots.Object,
            this.Builder.Object,
            new RankingSnapshotChecksumCalculator(),
            this.CacheInvalidator);
    }

    public RankingScopeDefinition Scope { get; }

    public Mock<IRatingRankingSourceRevisionRepository> Revisions { get; }

    public Mock<IRankingSnapshotRepository> Snapshots { get; }

    public Mock<IRatingRankingSnapshotBuilder> Builder { get; }

    public RecordingPublicationCacheInvalidator CacheInvalidator { get; }

    public RatingRankingRebuildScopeJobHandler Handler { get; }

    public DurableBackgroundJobExecutionContext CreateContext(
        long requestedRevision,
        bool forceRebuild = false)
    {
        RatingRankingRebuildScopePayload payload = new RatingRankingRebuildScopePayload(
            this.Scope.Key.Value,
            requestedRevision,
            this.Scope.MethodologyVersion.Value,
            forceRebuild);
        return new DurableBackgroundJobExecutionContext(
            "job-1",
            RatingRankingRebuildScopeJob.PayloadVersion,
            JsonSerializer.SerializeToElement(payload),
            requestedRevision,
            1,
            "correlation-1");
    }

    public void SetupUncoveredRevision(long requestedRevision)
    {
        this.Snapshots
            .Setup(repository => repository.GetPointerAsync(this.Scope.Key, CancellationToken.None))
            .ReturnsAsync((RankingPublicationPointer?)null);
        this.Revisions
            .Setup(repository => repository.GetAsync(this.Scope.Key, CancellationToken.None))
            .ReturnsAsync(this.CreateConvergedSourceRevision(this.Scope, requestedRevision));
        this.Revisions
            .Setup(repository => repository.GetAsync(
                CanonicalRankingScopes.GlobalParks.Key,
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                CanonicalRankingScopes.GlobalParks.Key,
                20,
                RatingRankingRebuildScopeJobHandlerTests.NowUtc));
    }

    public RatingRankingSourceRevision CreateConvergedSourceRevision(
        RankingScopeDefinition scope,
        long revision)
    {
        return new RatingRankingSourceRevision(
            scope.Key,
            revision,
            RatingRankingRebuildScopeJobHandlerTests.NowUtc,
            CacheConvergedMethodologyVersion: scope.MethodologyVersion,
            HighestCacheConvergedSourceRevision: revision);
    }

    public void SetupCacheConvergence(long requestedRevision)
    {
        this.Revisions
            .Setup(repository => repository.MarkCacheConvergedAsync(
                this.Scope.Key,
                this.Scope.MethodologyVersion,
                requestedRevision,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
    }

    public IReadOnlyCollection<RankingSnapshotEntry> CreateEligibleEntries()
    {
        RankingEvidence evidence = RankingEligibilityPolicy.Initial.EvaluateSimpleTarget(
            new SimpleRankingEvidenceInput(10, 10, true, false, true));
        return new[]
        {
            new RankingSnapshotEntry(1, 1, RatingTargetType.ParkItem, "item-1", ParkItemCategory.Attraction, 4.5, evidence),
            new RankingSnapshotEntry(2, 2, RatingTargetType.ParkItem, "item-2", ParkItemCategory.Attraction, 4.25, evidence),
            new RankingSnapshotEntry(3, 3, RatingTargetType.ParkItem, "item-3", ParkItemCategory.Attraction, 4.0, evidence),
        };
    }

    public RankingSnapshotHeader CreateHeader(
        RankingSnapshotStatus status,
        long sourceRevision = 5,
        int entryCount = 3,
        int buildAttempt = 1)
    {
        DateTime? validatedAtUtc = status is RankingSnapshotStatus.Validated
            or RankingSnapshotStatus.Current
            or RankingSnapshotStatus.Superseded
                ? RatingRankingRebuildScopeJobHandlerTests.NowUtc
                : null;
        DateTime? publishedAtUtc = status is RankingSnapshotStatus.Current
            or RankingSnapshotStatus.Superseded
                ? RatingRankingRebuildScopeJobHandlerTests.NowUtc
                : null;
        return new RankingSnapshotHeader(
            RankingSnapshotId.Parse("snapshot-1"),
            this.Scope.Key,
            this.Scope.MethodologyVersion,
            sourceRevision,
            status,
            entryCount,
            entryCount,
            this.Scope.PageSize,
            1,
            RatingRankingRebuildScopeJobHandlerTests.PlaceholderChecksum,
            RatingRankingRebuildScopeJobHandlerTests.NowUtc,
            validatedAtUtc,
            publishedAtUtc,
            buildAttempt: buildAttempt);
    }

    public RankingPublicationPointer CreatePointer(long revision)
    {
        return new RankingPublicationPointer(
            this.Scope.Key,
            RankingSnapshotId.Parse("snapshot-1"),
            RatingRankingRebuildScopeJobHandlerTests.NowUtc,
            null,
            null,
            this.Scope.MethodologyVersion,
            revision,
            revision,
            1,
            RatingRankingRebuildScopeJobHandlerTests.NowUtc);
    }

    public void VerifyNoCalls()
    {
        this.Revisions.VerifyNoOtherCalls();
        this.Snapshots.VerifyNoOtherCalls();
        this.Builder.VerifyNoOtherCalls();
    }
}
