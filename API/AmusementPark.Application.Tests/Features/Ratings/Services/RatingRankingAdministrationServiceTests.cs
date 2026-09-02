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
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RankingEligibilityPolicy policy = RankingEligibilityPolicy.Initial;
        ParkRankingEvidenceInput parkInput = new ParkRankingEvidenceInput(
            UniqueContributorCount: 18,
            RatingObservationCount: 58,
            DirectParkContributorCount: 8,
            ItemContributorCount: 10,
            ItemCategories: new[]
            {
                new RankingCategoryCoverage(3, 3),
                new RankingCategoryCoverage(2, 2),
            },
            IsSingleCategoryParkException: false,
            TargetCanReceiveVisitorRatings: true,
            IsExcludedByModeration: false,
            AggregateIntegrityIsValid: true);
        ParkRankingEvaluation parkEvaluation = policy.EvaluateParkRanking(parkInput, 4.2d, 4d);
        RatingRankingPolicyEvaluationEntry entry = new RatingRankingPolicyEvaluationEntry(
            RatingTargetType.Park,
            "park-a",
            "A",
            null,
            parkEvaluation.Score,
            parkEvaluation.Evidence,
            parkEvaluation.ItemComponent);
        Mock<IRankingScopeRegistry> registry = CreateRegistry(scope);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        RankingSnapshotHeader header = CreateCurrentHeader(scope, 1);
        snapshots.Setup(repository => repository.GetPointerAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(CreatePointer(header));
        snapshots.Setup(repository => repository.GetCurrentHeaderAsync(
                scope.Key,
                scope.MethodologyVersion,
                CancellationToken.None))
            .ReturnsAsync(header);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(repository => repository.GetAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                scope.Key,
                header.SourceRevision + 1,
                header.PublishedAtUtc!.Value.AddSeconds(1)));
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
        Assert.Equal(8, nearThreshold.UniqueContributorCount);
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
    public async Task GetDashboardAsync_WhenPublicationChangesBetweenReads_ShouldHideTheIncoherentState()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RankingSnapshotHeader currentHeader = CreateCurrentHeader(scope, 1);
        RankingSnapshotHeader previousHeader = CreateCurrentHeader(
            scope,
            1,
            sourceRevision: 6,
            snapshotId: "snapshot-previous");
        Mock<IRankingScopeRegistry> registry = CreateRegistry(scope);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(repository => repository.GetCurrentHeaderAsync(
                scope.Key,
                scope.MethodologyVersion,
                CancellationToken.None))
            .ReturnsAsync(currentHeader);
        snapshots.Setup(repository => repository.GetPointerAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(CreatePointer(previousHeader));
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(repository => repository.GetAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                scope.Key,
                currentHeader.SourceRevision,
                currentHeader.GeneratedAtUtc));
        Mock<IRatingRankingPolicyEvaluationBuilder> evaluator =
            new Mock<IRatingRankingPolicyEvaluationBuilder>(MockBehavior.Strict);
        evaluator.Setup(value => value.EvaluateAsync(
                scope,
                It.IsAny<RankingEligibilityPolicy>(),
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingPolicyEvaluationPlan(
                0,
                Array.Empty<RatingRankingPolicyEvaluationEntry>(),
                false));
        Mock<IRatingDiagnosticsReader> diagnosticsReader =
            new Mock<IRatingDiagnosticsReader>(MockBehavior.Strict);
        diagnosticsReader.Setup(reader => reader.GetDiagnosticsAsync(CancellationToken.None))
            .ReturnsAsync(CreateDiagnostics());
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

        RatingRankingScopeDiagnosticsResult diagnostics = Assert.Single(result.Scopes);
        Assert.Null(diagnostics.CurrentSnapshotId);
        Assert.Null(diagnostics.PublishedSourceRevision);
        Assert.Null(diagnostics.PublishedAtUtc);
        Assert.True(diagnostics.IsRebuildOutstanding);
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
    public async Task PreviewImpactAsync_WhenOnlyParkItemComponentIsIncomplete_ShouldCountTheComposition()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.GlobalParks;
        RankingEligibilityPolicy candidatePolicy = CreateCandidatePolicy();
        ParkRankingEvidenceInput parkInput = new ParkRankingEvidenceInput(
            UniqueContributorCount: 20,
            RatingObservationCount: 50,
            DirectParkContributorCount: 10,
            ItemContributorCount: 10,
            ItemCategories: new[]
            {
                new RankingCategoryCoverage(5, 4),
            },
            IsSingleCategoryParkException: true,
            TargetCanReceiveVisitorRatings: true,
            IsExcludedByModeration: false,
            AggregateIntegrityIsValid: true);
        ParkRankingEvaluation parkEvaluation = candidatePolicy.EvaluateParkRanking(
            parkInput,
            directParkScore: 4.2d,
            parkItemsScore: 3.8d);
        RatingRankingPolicyEvaluationEntry entry = new RatingRankingPolicyEvaluationEntry(
            RatingTargetType.Park,
            "park-a",
            "A",
            null,
            parkEvaluation.Score,
            parkEvaluation.Evidence,
            parkEvaluation.ItemComponent);
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
                It.Is<RankingEligibilityPolicy>(policy => policy.Version == candidatePolicy.Version),
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingPolicyEvaluationPlan(1, new[] { entry }, false));
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
        Assert.Equal(1, impact.IncompleteParkCompositionCount);
        Assert.True(parkEvaluation.Evidence.IsEligibleForMainRanking);
        Assert.Equal(
            RankingIneligibilityReason.InsufficientItemCoverage,
            parkEvaluation.ItemComponent.IneligibilityReason);
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
    public async Task PreviewImpactAsync_WhenRevisionDocumentIsStablyAbsent_ShouldCompareRevisionZeroSnapshot()
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.PublicItemCategories
            .Single(static definition => definition.Filter.ParkItemCategory == ParkItemCategory.Attraction);
        RankingSnapshotHeader header = CreateCurrentHeader(scope, 1, sourceRevision: 0);
        Mock<IRankingScopeRegistry> registry = CreateRegistry(scope);
        Mock<IRankingSnapshotRepository> snapshots =
            new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
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
            .ReturnsAsync(new RankingSnapshotPage(
                header,
                new[] { CreateSnapshotEntry(scope, 1, "item-a", 4.9d) },
                0,
                scope.PageSize));
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions.SetupSequence(repository => repository.GetAsync(scope.Key, CancellationToken.None))
            .ReturnsAsync((RatingRankingSourceRevision?)null)
            .ReturnsAsync((RatingRankingSourceRevision?)null);
        RankingEligibilityPolicy candidatePolicy = CreateCandidatePolicy();
        Mock<IRatingRankingPolicyEvaluationBuilder> evaluator =
            new Mock<IRatingRankingPolicyEvaluationBuilder>(MockBehavior.Strict);
        evaluator.Setup(value => value.EvaluateAsync(
                scope,
                It.Is<RankingEligibilityPolicy>(policy => policy.Version == candidatePolicy.Version),
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingPolicyEvaluationPlan(
                1,
                new[] { CreateEvaluationEntry(candidatePolicy, "item-a", "A", 4.9d, true) },
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
        Assert.True(impact.HasCurrentSnapshot);
        Assert.Equal(1, impact.ComparedRankCount);
        Assert.Equal(0, impact.GainedEligibilityCount);
        Assert.Equal(0, impact.LostEligibilityCount);
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
        long sourceRevision = 7,
        string snapshotId = "snapshot-current")
    {
        DateTime generatedAtUtc = new DateTime(2026, 9, 2, 11, 59, 0, DateTimeKind.Utc);
        DateTime publishedAtUtc = generatedAtUtc.AddSeconds(2);
        return new RankingSnapshotHeader(
            RankingSnapshotId.Parse(snapshotId),
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

    private static RankingPublicationPointer CreatePointer(RankingSnapshotHeader header)
    {
        return new RankingPublicationPointer(
            header.ScopeKey,
            header.Id,
            header.PublishedAtUtc!.Value,
            null,
            null,
            header.MethodologyVersion,
            header.SourceRevision,
            header.SourceRevision,
            version: 1,
            updatedAtUtc: header.PublishedAtUtc.Value);
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
