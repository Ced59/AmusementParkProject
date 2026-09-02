using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingRankingAdministrationServicesTests
{
    [Fact]
    public async Task GetDashboardAsync_ShouldExposeEvidenceThresholdAndSnapshotDiagnostics()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.PublicItemCategories
            .Single(static definition => definition.Filter.ParkItemCategory == ParkItemCategory.Attraction);
        RankingEligibilityPolicy policy = RankingEligibilityPolicy.Initial;
        RatingRankingPolicyEvaluationEntry entry = CreateEvaluationEntry(
            policy,
            "item-a",
            "A",
            4.2,
            false) with
        {
            Evidence = CreateSimpleEvidence(policy, 8, false),
        };
        Mock<IRankingScopeRegistry> registry = CreateRegistry(scope);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        RankingSnapshotHeader header = CreateCurrentHeader(scope, 1);
        snapshots.Setup(repository => repository.GetPointerAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync((RankingPublicationPointer?)null);
        snapshots.Setup(repository => repository.GetCurrentHeaderAsync(
                scope.Key,
                scope.MethodologyVersion,
                CancellationToken.None))
            .ReturnsAsync(header);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(repository => repository.GetAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync((RatingRankingSourceRevision?)null);
        Mock<IRatingRankingPolicyEvaluationBuilder> evaluator =
            new Mock<IRatingRankingPolicyEvaluationBuilder>(MockBehavior.Strict);
        evaluator.Setup(value => value.EvaluateAsync(
                scope,
                It.Is<RankingEligibilityPolicy>(candidate => candidate.Version == policy.Version),
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingPolicyEvaluationPlan(1, new[] { entry }, false));
        RatingDiagnosticsResult diagnostics = CreateDiagnostics();
        Mock<IRatingDiagnosticsReader> diagnosticsReader =
            new Mock<IRatingDiagnosticsReader>(MockBehavior.Strict);
        diagnosticsReader.Setup(reader => reader.GetDiagnosticsAsync(CancellationToken.None))
            .ReturnsAsync(diagnostics);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        List<DurableBackgroundJobDiagnosticQuery> diagnosticQueries =
            new List<DurableBackgroundJobDiagnosticQuery>();
        jobs.Setup(repository => repository.ListDiagnosticsAsync(
                It.IsAny<DurableBackgroundJobDiagnosticQuery>(),
                CancellationToken.None))
            .Callback<DurableBackgroundJobDiagnosticQuery, CancellationToken>(
                (query, _) => diagnosticQueries.Add(query))
            .ReturnsAsync(new[]
            {
                new DurableBackgroundJobDiagnosticItem(
                    "job-1",
                    RatingRankingRebuildScopeJob.Kind,
                    RatingRankingRebuildScopeJob.BuildNaturalKey(scope.Key),
                    DurableBackgroundJobStatus.Succeeded,
                    0,
                    1,
                    header.SourceRevision,
                    header.SourceRevision,
                    header.GeneratedAtUtc.AddSeconds(-30),
                    null,
                    header.GeneratedAtUtc.AddSeconds(-30),
                    header.PublishedAtUtc!.Value.AddSeconds(1),
                    header.PublishedAtUtc!.Value.AddSeconds(1),
                    null,
                    null),
            });
        RatingRankingAdministrationDashboardReader reader =
            new RatingRankingAdministrationDashboardReader(
                registry.Object,
                snapshots.Object,
                revisions.Object,
                evaluator.Object,
                diagnosticsReader.Object,
                jobs.Object);

        RatingRankingAdministrationResult result = await reader.GetDashboardAsync(
            CancellationToken.None);

        Assert.Same(diagnostics, result.DataDiagnostics);
        RatingRankingScopeDiagnosticsResult scopeDiagnostics = Assert.Single(result.Scopes);
        Assert.True(scopeDiagnostics.IsRebuildOutstanding);
        Assert.Equal(32_000, scopeDiagnostics.RebuildDurationMilliseconds);
        RatingRankingNearThresholdTargetResult nearThreshold =
            Assert.Single(result.NearThresholdTargets);
        Assert.Equal(2, nearThreshold.RemainingContributorCount);
        Assert.Equal(RankingEvidenceLevel.Provisional, Assert.Single(result.EvidenceDistribution).Level);
        Assert.Equal(
            RankingIneligibilityReason.TooFewUniqueContributors,
            Assert.Single(result.Exclusions).Reason);
        DurableBackgroundJobDiagnosticQuery diagnosticQuery = Assert.Single(diagnosticQueries);
        Assert.Equal(RatingRankingRebuildScopeJob.BuildNaturalKey(scope.Key), diagnosticQuery.NaturalKey);
        Assert.Equal(1, diagnosticQuery.Limit);
        snapshots.VerifyAll();
        revisions.VerifyAll();
        evaluator.VerifyAll();
        diagnosticsReader.VerifyAll();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task PreviewImpactAsync_ShouldCompareCandidateEligibilityWithTheCurrentSnapshot()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.PublicItemCategories
            .Single(static definition => definition.Filter.ParkItemCategory == ParkItemCategory.Attraction);
        RankingSnapshotHeader header = CreateCurrentHeader(scope, 3);
        IReadOnlyCollection<RankingSnapshotEntry> currentEntries = new[]
        {
            CreateSnapshotEntry(scope, 1, "item-a", 4.9),
            CreateSnapshotEntry(scope, 2, "item-b", 4.8),
            CreateSnapshotEntry(scope, 3, "item-d", 4.6),
        };
        RankingEligibilityPolicy candidatePolicy = CreateCandidatePolicy();
        IReadOnlyCollection<RatingRankingPolicyEvaluationEntry> candidateEntries = new[]
        {
            CreateEvaluationEntry(candidatePolicy, "item-b", "B", 4.8, true),
            CreateEvaluationEntry(candidatePolicy, "item-c", "C", 4.7, true),
            CreateEvaluationEntry(candidatePolicy, "item-d", "D", 4.6, true),
            CreateEvaluationEntry(candidatePolicy, "item-a", "A", 4.9, false),
        };
        Mock<IRankingScopeRegistry> registry = CreateRegistry(scope);
        Mock<IRankingSnapshotRepository> snapshots = new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(repository => repository.GetCurrentHeaderAsync(
                scope.Key,
                scope.MethodologyVersion,
                CancellationToken.None))
            .ReturnsAsync(header);
        snapshots.Setup(repository => repository.GetCurrentPageAsync(
                scope.Key,
                scope.MethodologyVersion,
                0,
                scope.PageSize,
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotPage(header, currentEntries, 0, scope.PageSize));
        Mock<IRatingRankingPolicyEvaluationBuilder> evaluator =
            new Mock<IRatingRankingPolicyEvaluationBuilder>(MockBehavior.Strict);
        evaluator.Setup(value => value.EvaluateAsync(
                scope,
                It.Is<RankingEligibilityPolicy>(policy => policy.Version == candidatePolicy.Version),
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingPolicyEvaluationPlan(4, candidateEntries, false));
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            CreateStableRevisionRepository(scope, header.SourceRevision);
        RatingRankingPolicyImpactPreviewer previewer = CreatePreviewer(
            registry.Object,
            snapshots.Object,
            revisions.Object,
            evaluator.Object);
        RatingRankingPolicyCandidate candidate = CreateCandidate();

        RatingRankingPolicyImpactResult result = await previewer.PreviewImpactAsync(
            candidate,
            CancellationToken.None);

        RatingRankingPolicyScopeImpactResult impact = Assert.Single(result.Scopes);
        Assert.True(impact.HasCurrentSnapshot);
        Assert.Equal(1, impact.GainedEligibilityCount);
        Assert.Equal(1, impact.LostEligibilityCount);
        Assert.Equal(2, impact.ComparedRankCount);
        Assert.Equal(1, impact.TotalAbsoluteRankChange);
        Assert.Equal(1, impact.MaximumRankChange);
        Assert.Equal(0.5d, impact.AverageRankChange);
        Assert.Equal("item-c", Assert.Single(impact.GainedTargets).TargetId);
        Assert.Equal("item-a", Assert.Single(impact.LostTargets).TargetId);
        Assert.True(impact.HasMinimumComparableEntries);
        snapshots.VerifyAll();
        revisions.VerifyAll();
        evaluator.VerifyAll();
    }

    [Fact]
    public async Task RequestRebuildAsync_ShouldAdvanceEveryScopeRevisionBeforeScheduling()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        Mock<IRankingScopeRegistry> registry = CreateRegistry(scope);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        RatingRankingMutationLease lease = RatingRankingMutationLease.Create(scope.Key);
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(
            scope.Key,
            8,
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc));
        revisions.Setup(repository => repository.BeginMutationAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(lease);
        revisions.Setup(repository => repository.CompleteMutationAsync(
                lease,
                true,
                CancellationToken.None))
            .ReturnsAsync(revision);
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        scheduler.Setup(value => value.ScheduleIfOutstandingAsync(revision, CancellationToken.None))
            .ReturnsAsync(RatingRankingRebuildScheduleDisposition.Scheduled);
        RatingRankingRebuildRequester requester = new RatingRankingRebuildRequester(
            registry.Object,
            revisions.Object,
            scheduler.Object);

        RatingRankingRebuildRequestResult result = await requester.RequestRebuildAsync(
            CancellationToken.None);

        Assert.Equal(1, result.ScheduledScopeCount);
        Assert.Equal(8, Assert.Single(result.Scopes).RequestedSourceRevision);
        revisions.VerifyAll();
        scheduler.VerifyAll();
    }

    [Fact]
    public async Task PreviewImpactAsync_ShouldNotReportFalseLossesWhenTheSourceIsTruncated()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.PublicItemCategories
            .Single(static definition => definition.Filter.ParkItemCategory == ParkItemCategory.Attraction);
        Mock<IRankingScopeRegistry> registry = CreateRegistry(scope);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(repository => repository.GetCurrentHeaderAsync(
                scope.Key,
                scope.MethodologyVersion,
                CancellationToken.None))
            .ReturnsAsync((RankingSnapshotHeader?)null);
        Mock<IRatingRankingPolicyEvaluationBuilder> evaluator =
            new Mock<IRatingRankingPolicyEvaluationBuilder>(MockBehavior.Strict);
        evaluator.Setup(value => value.EvaluateAsync(
                scope,
                It.IsAny<RankingEligibilityPolicy>(),
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingPolicyEvaluationPlan(
                50_000,
                Array.Empty<RatingRankingPolicyEvaluationEntry>(),
                true));
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            CreateStableRevisionRepository(scope, 7);
        RatingRankingPolicyImpactPreviewer previewer = CreatePreviewer(
            registry.Object,
            snapshots.Object,
            revisions.Object,
            evaluator.Object);

        RatingRankingPolicyImpactResult result = await previewer.PreviewImpactAsync(
            CreateCandidate(),
            CancellationToken.None);

        RatingRankingPolicyScopeImpactResult impact = Assert.Single(result.Scopes);
        Assert.False(impact.IsImpactAvailable);
        Assert.True(impact.IsSourceTruncated);
        Assert.Equal(0, impact.GainedEligibilityCount);
        Assert.Equal(0, impact.LostEligibilityCount);
        Assert.Null(impact.AverageRankChange);
        Assert.Equal(0, result.ScopeCountBelowMinimum);
        snapshots.VerifyAll();
        revisions.VerifyAll();
        evaluator.VerifyAll();
    }

    [Fact]
    public async Task PreviewImpactAsync_WhenCurrentSnapshotChangesDuringPaging_ShouldRejectTheComparison()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.PublicItemCategories
            .Single(static definition => definition.Filter.ParkItemCategory == ParkItemCategory.Attraction);
        RankingSnapshotHeader initialHeader = CreateCurrentHeader(scope, 1);
        RankingSnapshotHeader replacementHeader = new RankingSnapshotHeader(
            RankingSnapshotId.Parse("snapshot-replacement"),
            scope.Key,
            scope.MethodologyVersion,
            8,
            RankingSnapshotStatus.Current,
            1,
            1,
            scope.PageSize,
            1,
            RankingSnapshotChecksum.Parse(new string('1', RankingSnapshotChecksum.HexadecimalLength)),
            initialHeader.GeneratedAtUtc.AddMinutes(1),
            initialHeader.ValidatedAtUtc!.Value.AddMinutes(1),
            initialHeader.PublishedAtUtc!.Value.AddMinutes(1));
        Mock<IRankingScopeRegistry> registry = CreateRegistry(scope);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(repository => repository.GetCurrentHeaderAsync(
                scope.Key,
                scope.MethodologyVersion,
                CancellationToken.None))
            .ReturnsAsync(initialHeader);
        snapshots.Setup(repository => repository.GetCurrentPageAsync(
                scope.Key,
                scope.MethodologyVersion,
                0,
                scope.PageSize,
                CancellationToken.None))
            .ReturnsAsync(new RankingSnapshotPage(
                replacementHeader,
                new[] { CreateSnapshotEntry(scope, 1, "item-a", 4.9) },
                0,
                scope.PageSize));
        RankingEligibilityPolicy candidatePolicy = CreateCandidatePolicy();
        Mock<IRatingRankingPolicyEvaluationBuilder> evaluator =
            new Mock<IRatingRankingPolicyEvaluationBuilder>(MockBehavior.Strict);
        evaluator.Setup(value => value.EvaluateAsync(
                scope,
                It.Is<RankingEligibilityPolicy>(policy => policy.Version == candidatePolicy.Version),
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingPolicyEvaluationPlan(
                1,
                new[] { CreateEvaluationEntry(candidatePolicy, "item-a", "A", 4.9, true) },
                false));
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            CreateStableRevisionRepository(scope, initialHeader.SourceRevision);
        RatingRankingPolicyImpactPreviewer previewer = CreatePreviewer(
            registry.Object,
            snapshots.Object,
            revisions.Object,
            evaluator.Object);

        RatingRankingPolicyImpactResult result = await previewer.PreviewImpactAsync(
            CreateCandidate(),
            CancellationToken.None);

        RatingRankingPolicyScopeImpactResult impact = Assert.Single(result.Scopes);
        Assert.False(impact.HasCurrentSnapshot);
        Assert.Equal(0, impact.GainedEligibilityCount);
        Assert.Equal(0, impact.LostEligibilityCount);
        Assert.Equal(0, impact.ComparedRankCount);
        snapshots.VerifyAll();
        revisions.VerifyAll();
        evaluator.VerifyAll();
    }

    [Fact]
    public async Task PreviewImpactAsync_WhenSourceRevisionChangesDuringEvaluation_ShouldRejectTheComparison()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.PublicItemCategories
            .Single(static definition => definition.Filter.ParkItemCategory == ParkItemCategory.Attraction);
        RankingSnapshotHeader header = CreateCurrentHeader(scope, 1, sourceRevision: 8);
        Mock<IRankingScopeRegistry> registry = CreateRegistry(scope);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(repository => repository.GetCurrentHeaderAsync(
                scope.Key,
                scope.MethodologyVersion,
                CancellationToken.None))
            .ReturnsAsync(header);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions.SetupSequence(repository => repository.GetAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(scope.Key, 7, header.GeneratedAtUtc.AddMinutes(-1)))
            .ReturnsAsync(new RatingRankingSourceRevision(scope.Key, 8, header.GeneratedAtUtc));
        RankingEligibilityPolicy candidatePolicy = CreateCandidatePolicy();
        Mock<IRatingRankingPolicyEvaluationBuilder> evaluator =
            new Mock<IRatingRankingPolicyEvaluationBuilder>(MockBehavior.Strict);
        evaluator.Setup(value => value.EvaluateAsync(
                scope,
                It.Is<RankingEligibilityPolicy>(policy => policy.Version == candidatePolicy.Version),
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingPolicyEvaluationPlan(
                1,
                new[] { CreateEvaluationEntry(candidatePolicy, "item-a", "A", 4.9, true) },
                false));
        RatingRankingPolicyImpactPreviewer previewer = CreatePreviewer(
            registry.Object,
            snapshots.Object,
            revisions.Object,
            evaluator.Object);

        RatingRankingPolicyImpactResult result = await previewer.PreviewImpactAsync(
            CreateCandidate(),
            CancellationToken.None);

        RatingRankingPolicyScopeImpactResult impact = Assert.Single(result.Scopes);
        Assert.False(impact.HasCurrentSnapshot);
        Assert.Equal(0, impact.GainedEligibilityCount);
        Assert.Equal(0, impact.LostEligibilityCount);
        Assert.Equal(0, impact.ComparedRankCount);
        snapshots.VerifyAll();
        revisions.VerifyAll();
        evaluator.VerifyAll();
    }

    [Fact]
    public async Task RequestRebuildAsync_WhenSchedulingIsDeferred_ShouldNotReportTheScopeAsScheduled()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        Mock<IRankingScopeRegistry> registry = CreateRegistry(scope);
        RatingRankingMutationLease lease = RatingRankingMutationLease.Create(scope.Key);
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(
            scope.Key,
            8,
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc),
            PendingMutationCount: 1);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(repository => repository.BeginMutationAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(lease);
        revisions.Setup(repository => repository.CompleteMutationAsync(
                lease,
                true,
                CancellationToken.None))
            .ReturnsAsync(revision);
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        scheduler.Setup(value => value.ScheduleIfOutstandingAsync(revision, CancellationToken.None))
            .ReturnsAsync(RatingRankingRebuildScheduleDisposition.Deferred);
        RatingRankingRebuildRequester requester = new RatingRankingRebuildRequester(
            registry.Object,
            revisions.Object,
            scheduler.Object);

        RatingRankingRebuildRequestResult result = await requester.RequestRebuildAsync(
            CancellationToken.None);

        Assert.Equal(0, result.ScheduledScopeCount);
        Assert.Empty(result.Scopes);
        revisions.VerifyAll();
        scheduler.VerifyAll();
    }

    private static RatingRankingPolicyImpactPreviewer CreatePreviewer(
        IRankingScopeRegistry registry,
        IRankingSnapshotRepository? snapshotRepository = null,
        IRatingRankingSourceRevisionRepository? sourceRevisionRepository = null,
        IRatingRankingPolicyEvaluationBuilder? evaluator = null)
    {
        return new RatingRankingPolicyImpactPreviewer(
            registry,
            snapshotRepository ?? Mock.Of<IRankingSnapshotRepository>(),
            sourceRevisionRepository ?? Mock.Of<IRatingRankingSourceRevisionRepository>(),
            evaluator ?? Mock.Of<IRatingRankingPolicyEvaluationBuilder>());
    }

    private static Mock<IRatingRankingSourceRevisionRepository> CreateStableRevisionRepository(
        RankingScopeDefinition scope,
        long sourceRevision)
    {
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        RatingRankingSourceRevision revision = new RatingRankingSourceRevision(
            scope.Key,
            sourceRevision,
            new DateTime(2026, 9, 2, 11, 59, 0, DateTimeKind.Utc));
        revisions.SetupSequence(repository => repository.GetAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(revision)
            .ReturnsAsync(revision);
        return revisions;
    }

    private static Mock<IRankingScopeRegistry> CreateRegistry(RankingScopeDefinition scope)
    {
        Mock<IRankingScopeRegistry> registry = new Mock<IRankingScopeRegistry>(MockBehavior.Strict);
        registry.SetupGet(value => value.Definitions).Returns(new[] { scope });
        return registry;
    }

    private static RatingRankingPolicyCandidate CreateCandidate()
    {
        return new RatingRankingPolicyCandidate(
            "ratings-2026-02",
            3,
            10,
            30,
            100,
            3,
            5,
            2,
            2,
            0.0001m);
    }

    private static RatingDiagnosticsResult CreateDiagnostics()
    {
        return new RatingDiagnosticsResult(
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc),
            10,
            8,
            10,
            new[] { "0.5", "1" },
            false,
            new RatingAnomalySummaryResult(0, 0, 0, 0, 0, 0, 0, 0, 0),
            new RatingAggregateIntegrityResult(true, true, 1, 0, 0, 0, 0, 0),
            Array.Empty<RatingTargetDistributionResult>(),
            Array.Empty<RatingIndexStatusResult>());
    }

    private static RankingEligibilityPolicy CreateCandidatePolicy()
    {
        return CreateCandidate().ToDomain();
    }

    private static RankingSnapshotHeader CreateCurrentHeader(
        RankingScopeDefinition scope,
        int eligibleEntryCount,
        long sourceRevision = 7)
    {
        DateTime generatedAtUtc = new DateTime(2026, 9, 2, 11, 59, 0, DateTimeKind.Utc);
        DateTime publishedAtUtc = generatedAtUtc.AddSeconds(2);
        return new RankingSnapshotHeader(
            RankingSnapshotId.Parse("snapshot-current"),
            scope.Key,
            scope.MethodologyVersion,
            sourceRevision,
            RankingSnapshotStatus.Current,
            eligibleEntryCount,
            eligibleEntryCount,
            scope.PageSize,
            1,
            RankingSnapshotChecksum.Parse(new string('0', RankingSnapshotChecksum.HexadecimalLength)),
            generatedAtUtc,
            generatedAtUtc.AddSeconds(1),
            publishedAtUtc);
    }

    private static RankingSnapshotEntry CreateSnapshotEntry(
        RankingScopeDefinition scope,
        int rank,
        string targetId,
        double score)
    {
        RankingEvidence evidence = CreateSimpleEvidence(RankingEligibilityPolicy.Initial, 10, true);
        return new RankingSnapshotEntry(
            rank,
            rank,
            RatingTargetType.ParkItem,
            targetId,
            scope.Filter.ParkItemCategory,
            score,
            evidence);
    }

    private static RatingRankingPolicyEvaluationEntry CreateEvaluationEntry(
        RankingEligibilityPolicy policy,
        string targetId,
        string targetName,
        double score,
        bool eligible)
    {
        int contributorCount = eligible ? policy.EligibleMinUniqueContributors : 2;
        return new RatingRankingPolicyEvaluationEntry(
            RatingTargetType.ParkItem,
            targetId,
            targetName,
            ParkItemCategory.Attraction,
            score,
            CreateSimpleEvidence(policy, contributorCount, eligible));
    }

    private static RankingEvidence CreateSimpleEvidence(
        RankingEligibilityPolicy policy,
        int contributorCount,
        bool expectedEligibility)
    {
        bool evaluated = policy.TryEvaluateSimpleTarget(
            new SimpleRankingEvidenceInput(
                contributorCount,
                contributorCount,
                TargetCanReceiveVisitorRatings: true,
                IsExcludedByModeration: false,
                AggregateIntegrityIsValid: true),
            out RankingEvidence? evidence);
        Assert.True(evaluated);
        Assert.NotNull(evidence);
        Assert.Equal(expectedEligibility, evidence.IsEligibleForMainRanking);
        return evidence;
    }
}
