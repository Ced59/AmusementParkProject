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
        snapshots.Setup(repository => repository.GetPointerAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync((RankingPublicationPointer?)null);
        snapshots.Setup(repository => repository.GetCurrentHeaderAsync(
                scope.Key,
                scope.MethodologyVersion,
                CancellationToken.None))
            .ReturnsAsync((RankingSnapshotHeader?)null);
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
        jobs.Setup(repository => repository.ListDiagnosticsAsync(
                It.IsAny<DurableBackgroundJobDiagnosticQuery>(),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<DurableBackgroundJobDiagnosticItem>());
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
        Assert.True(Assert.Single(result.Scopes).IsRebuildOutstanding);
        RatingRankingNearThresholdTargetResult nearThreshold =
            Assert.Single(result.NearThresholdTargets);
        Assert.Equal(2, nearThreshold.RemainingContributorCount);
        Assert.Equal(RankingEvidenceLevel.Provisional, Assert.Single(result.EvidenceDistribution).Level);
        Assert.Equal(
            RankingIneligibilityReason.TooFewUniqueContributors,
            Assert.Single(result.Exclusions).Reason);
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
        RatingRankingPolicyImpactPreviewer previewer = CreatePreviewer(
            registry.Object,
            snapshots.Object,
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
            .Returns(Task.CompletedTask);
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
        RatingRankingPolicyImpactPreviewer previewer = CreatePreviewer(
            registry.Object,
            snapshots.Object,
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
        evaluator.VerifyAll();
    }

    private static RatingRankingPolicyImpactPreviewer CreatePreviewer(
        IRankingScopeRegistry registry,
        IRankingSnapshotRepository? snapshotRepository = null,
        IRatingRankingPolicyEvaluationBuilder? evaluator = null)
    {
        return new RatingRankingPolicyImpactPreviewer(
            registry,
            snapshotRepository ?? Mock.Of<IRankingSnapshotRepository>(),
            evaluator ?? Mock.Of<IRatingRankingPolicyEvaluationBuilder>());
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
        int eligibleEntryCount)
    {
        DateTime generatedAtUtc = new DateTime(2026, 9, 2, 11, 59, 0, DateTimeKind.Utc);
        DateTime publishedAtUtc = generatedAtUtc.AddSeconds(2);
        return new RankingSnapshotHeader(
            RankingSnapshotId.Parse("snapshot-current"),
            scope.Key,
            scope.MethodologyVersion,
            7,
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
