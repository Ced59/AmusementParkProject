using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RatingRankingAdministrationDashboardReader
{
    private const int NearEligibilityMaximumGap = 3;
    private const int MaximumHighlightedTargets = 50;

    private readonly IRankingScopeRegistry scopeRegistry;
    private readonly IRankingSnapshotRepository snapshotRepository;
    private readonly IRatingRankingSourceRevisionRepository sourceRevisionRepository;
    private readonly IRatingRankingPolicyEvaluationBuilder policyEvaluationBuilder;
    private readonly IRatingDiagnosticsReader diagnosticsReader;
    private readonly IDurableBackgroundJobRepository backgroundJobRepository;
    private readonly TimeProvider timeProvider;

    public RatingRankingAdministrationDashboardReader(
        IRankingScopeRegistry scopeRegistry,
        IRankingSnapshotRepository snapshotRepository,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository,
        IRatingRankingPolicyEvaluationBuilder policyEvaluationBuilder,
        IRatingDiagnosticsReader diagnosticsReader,
        IDurableBackgroundJobRepository backgroundJobRepository,
        TimeProvider? timeProvider = null)
    {
        this.scopeRegistry = scopeRegistry;
        this.snapshotRepository = snapshotRepository;
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.policyEvaluationBuilder = policyEvaluationBuilder;
        this.diagnosticsReader = diagnosticsReader;
        this.backgroundJobRepository = backgroundJobRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<RatingRankingAdministrationResult> GetDashboardAsync(
        CancellationToken cancellationToken)
    {
        RatingMethodologyDefinition methodology = RatingMethodologyCatalog.Current;
        RankingEligibilityPolicy policy = methodology.EligibilityPolicy;
        RatingDiagnosticsResult dataDiagnostics = await this.diagnosticsReader.GetDiagnosticsAsync(
            cancellationToken);
        List<RatingRankingScopeDiagnosticsResult> scopes = new List<RatingRankingScopeDiagnosticsResult>();
        List<RatingRankingPolicyEvaluationEntry> evaluatedEntries =
            new List<RatingRankingPolicyEvaluationEntry>();
        List<RatingRankingNearThresholdTargetResult> nearThresholdTargets =
            new List<RatingRankingNearThresholdTargetResult>();
        List<RatingRankingCategoryCoverageResult> categoryCoverage =
            new List<RatingRankingCategoryCoverageResult>();

        foreach (RankingScopeDefinition scope in this.scopeRegistry.Definitions
                     .OrderBy(static definition => definition.Key.Value, StringComparer.Ordinal))
        {
            RankingSnapshotHeader? header = await this.snapshotRepository.GetCurrentHeaderAsync(
                scope.Key,
                scope.MethodologyVersion,
                cancellationToken);
            RankingPublicationPointer? pointer = await this.snapshotRepository.GetPointerAsync(
                scope.Key,
                cancellationToken);
            if (!PublicationStateMatches(scope, header, pointer))
            {
                header = null;
                pointer = null;
            }
            RatingRankingSourceRevision? sourceRevisionBeforeEvaluation =
                await this.sourceRevisionRepository.GetAsync(scope.Key, cancellationToken);
            DurableBackgroundJobDiagnosticItem? latestJob = await this.GetLatestScopeJobAsync(
                scope.Key,
                cancellationToken);
            DurableBackgroundJobDiagnosticItem? publishedSnapshotJob =
                IsSuccessfulJobForHeader(latestJob, header)
                    ? latestJob
                    : await this.GetPublishedSnapshotJobAsync(
                        scope.Key,
                        header,
                        cancellationToken);
            RatingRankingPolicyEvaluationPlan evaluation =
                await this.policyEvaluationBuilder.EvaluateAsync(scope, policy, cancellationToken);
            RatingRankingSourceRevision? sourceRevisionAfterEvaluation =
                await this.sourceRevisionRepository.GetAsync(scope.Key, cancellationToken);
            bool isDiagnosticSourceStable = SourceRevisionsMatch(
                sourceRevisionBeforeEvaluation,
                sourceRevisionAfterEvaluation);
            RatingRankingSourceRevision? sourceRevision =
                sourceRevisionAfterEvaluation ?? sourceRevisionBeforeEvaluation;
            long resolvedSourceRevision = sourceRevision?.Revision ?? 0;
            bool isOutstanding = !isDiagnosticSourceStable
                || sourceRevision is null
                || !sourceRevision.IsRebuildable
                || pointer is null
                || pointer.MethodologyVersion != scope.MethodologyVersion
                || pointer.SourceRevision != resolvedSourceRevision;
            long? durationMilliseconds = publishedSnapshotJob?.CompletedAtUtc is DateTime completedAtUtc
                ? Math.Max(
                    0,
                    checked((long)(completedAtUtc - publishedSnapshotJob.CreatedAtUtc).TotalMilliseconds))
                : null;
            scopes.Add(new RatingRankingScopeDiagnosticsResult(
                scope.Key.Value,
                scope.TargetFamily,
                scope.Filter.ParkItemCategory,
                scope.MethodologyVersion.Value,
                header?.Id.Value,
                header?.GeneratedAtUtc,
                header?.PublishedAtUtc,
                durationMilliseconds,
                header?.TotalEntryCount ?? 0,
                header?.EligibleEntryCount ?? 0,
                resolvedSourceRevision,
                pointer?.SourceRevision,
                isOutstanding,
                evaluation.IsSourceTruncated,
                latestJob?.Status.ToString(),
                latestJob?.LastErrorCode,
                latestJob?.UpdatedAtUtc));

            if (!isDiagnosticSourceStable || evaluation.IsSourceTruncated)
            {
                continue;
            }

            evaluatedEntries.AddRange(evaluation.Entries);
            AddNearThresholdTargets(scope, policy, evaluation.Entries, nearThresholdTargets);
            if (scope.Filter.ParkItemCategory is ParkItemCategory category)
            {
                int eligibleCount = evaluation.Entries.Count(
                    static entry => entry.Evidence?.IsEligibleForMainRanking == true);
                categoryCoverage.Add(new RatingRankingCategoryCoverageResult(
                    scope.Key.Value,
                    category,
                    evaluation.TotalEntryCount,
                    eligibleCount,
                    eligibleCount >= policy.MinimumEligibleEntriesPerRanking));
            }
        }

        IReadOnlyCollection<RatingRankingEvidenceDistributionResult> evidenceDistribution =
            evaluatedEntries
                .GroupBy(static entry => new
                {
                    entry.TargetType,
                    Level = entry.Evidence?.Level ?? RankingEvidenceLevel.Excluded,
                })
                .Select(static group => new RatingRankingEvidenceDistributionResult(
                    group.Key.TargetType,
                    group.Key.Level,
                    group.Count(),
                    group.Sum(static entry => (long)(entry.Evidence?.UniqueContributorCount ?? 0)),
                    group.Sum(static entry => (long)(entry.Evidence?.RatingObservationCount ?? 0))))
                .OrderBy(static result => result.TargetType)
                .ThenBy(static result => result.Level)
                .ToArray();
        IReadOnlyCollection<RatingRankingExclusionDistributionResult> exclusions = evaluatedEntries
            .Where(static entry => entry.Evidence?.IsEligibleForMainRanking != true)
            .GroupBy(static entry => new
            {
                entry.TargetType,
                Reason = entry.Evidence?.IneligibilityReason
                    ?? RankingIneligibilityReason.UnsupportedComposition,
            })
            .Select(static group => new RatingRankingExclusionDistributionResult(
                group.Key.TargetType,
                group.Key.Reason,
                group.Count()))
            .OrderBy(static result => result.TargetType)
            .ThenBy(static result => result.Reason)
            .ToArray();

        return new RatingRankingAdministrationResult(
            this.GetUtcNow(),
            RatingMethodologyResultFactory.Create(methodology),
            null,
            dataDiagnostics,
            scopes.AsReadOnly(),
            evidenceDistribution,
            nearThresholdTargets
                .OrderBy(static target => target.RemainingContributorCount)
                .ThenBy(static target => target.TargetName, StringComparer.OrdinalIgnoreCase)
                .Take(MaximumHighlightedTargets)
                .ToArray(),
            exclusions,
            categoryCoverage.AsReadOnly());
    }

    private static void AddNearThresholdTargets(
        RankingScopeDefinition scope,
        RankingEligibilityPolicy policy,
        IEnumerable<RatingRankingPolicyEvaluationEntry> entries,
        ICollection<RatingRankingNearThresholdTargetResult> targets)
    {
        foreach (RatingRankingPolicyEvaluationEntry entry in entries)
        {
            RankingEvidence? evidence = entry.Evidence;
            if (evidence is null
                || evidence.IsEligibleForMainRanking
                || evidence.IneligibilityReason != RankingIneligibilityReason.TooFewUniqueContributors)
            {
                continue;
            }

            int eligibilityContributorCount = policy.ResolveMainRankingEligibilityContributorCount(
                entry.TargetType,
                evidence);
            int remainingContributorCount = policy.EligibleMinUniqueContributors
                - eligibilityContributorCount;
            if (remainingContributorCount <= 0
                || remainingContributorCount > NearEligibilityMaximumGap)
            {
                continue;
            }

            targets.Add(new RatingRankingNearThresholdTargetResult(
                scope.Key.Value,
                entry.TargetType,
                entry.TargetId,
                entry.TargetName,
                eligibilityContributorCount,
                policy.EligibleMinUniqueContributors,
                remainingContributorCount));
        }
    }

    private async Task<DurableBackgroundJobDiagnosticItem?> GetLatestScopeJobAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken)
    {
        List<DurableBackgroundJobDiagnosticItem> jobs = new List<DurableBackgroundJobDiagnosticItem>();
        foreach (string naturalKey in GetRebuildNaturalKeys(scopeKey))
        {
            IReadOnlyCollection<DurableBackgroundJobDiagnosticItem> matchingJobs =
                await this.backgroundJobRepository.ListDiagnosticsAsync(
                    new DurableBackgroundJobDiagnosticQuery(
                        Kind: RatingRankingRebuildScopeJob.Kind,
                        Limit: 1,
                        NaturalKey: naturalKey),
                    cancellationToken);
            jobs.AddRange(matchingJobs);
        }

        return jobs
            .OrderByDescending(static job => job.UpdatedAtUtc)
            .FirstOrDefault();
    }

    private async Task<DurableBackgroundJobDiagnosticItem?> GetPublishedSnapshotJobAsync(
        RankingScopeKey scopeKey,
        RankingSnapshotHeader? header,
        CancellationToken cancellationToken)
    {
        if (header is null)
        {
            return null;
        }

        List<DurableBackgroundJobDiagnosticItem> jobs = new List<DurableBackgroundJobDiagnosticItem>();
        foreach (string naturalKey in GetRebuildNaturalKeys(scopeKey))
        {
            IReadOnlyCollection<DurableBackgroundJobDiagnosticItem> matchingJobs =
                await this.backgroundJobRepository.ListDiagnosticsAsync(
                    new DurableBackgroundJobDiagnosticQuery(
                        Statuses: new[] { DurableBackgroundJobStatus.Succeeded },
                        Kind: RatingRankingRebuildScopeJob.Kind,
                        Limit: 1,
                        NaturalKey: naturalKey,
                        ProcessedRevision: header.SourceRevision,
                        MaximumCreatedAtUtc: header.GeneratedAtUtc),
                    cancellationToken);
            jobs.AddRange(matchingJobs);
        }

        return jobs
            .Where(job => job.CreatedAtUtc <= header.GeneratedAtUtc)
            .OrderByDescending(static job => job.CompletedAtUtc ?? job.UpdatedAtUtc)
            .FirstOrDefault();
    }

    private static IReadOnlyCollection<string> GetRebuildNaturalKeys(RankingScopeKey scopeKey)
    {
        return new[]
        {
            RatingRankingRebuildScopeJob.BuildNaturalKey(scopeKey),
            RatingRankingRebuildScopeJob.BuildForcedNaturalKey(scopeKey),
        };
    }

    private static bool IsSuccessfulJobForHeader(
        DurableBackgroundJobDiagnosticItem? job,
        RankingSnapshotHeader? header)
    {
        return job is not null
            && header is not null
            && job.Status == DurableBackgroundJobStatus.Succeeded
            && job.ProcessedRevision == header.SourceRevision
            && job.CreatedAtUtc <= header.GeneratedAtUtc
            && job.CompletedAtUtc.HasValue;
    }

    private static bool PublicationStateMatches(
        RankingScopeDefinition scope,
        RankingSnapshotHeader? header,
        RankingPublicationPointer? pointer)
    {
        if (header is null || pointer is null)
        {
            return header is null && pointer is null;
        }

        return header.Id == pointer.CurrentSnapshotId
            && header.ScopeKey == scope.Key
            && pointer.ScopeKey == scope.Key
            && header.MethodologyVersion == scope.MethodologyVersion
            && pointer.MethodologyVersion == scope.MethodologyVersion
            && header.SourceRevision == pointer.SourceRevision;
    }

    private static bool SourceRevisionsMatch(
        RatingRankingSourceRevision? before,
        RatingRankingSourceRevision? after)
    {
        if (before is null || after is null)
        {
            return before is null && after is null;
        }

        return before.ScopeKey == after.ScopeKey
            && before.Revision == after.Revision
            && before.IsRebuildable
            && after.IsRebuildable;
    }

    private DateTime GetUtcNow()
    {
        return this.timeProvider.GetUtcNow().UtcDateTime;
    }
}
