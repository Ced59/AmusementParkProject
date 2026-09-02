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
        IReadOnlyCollection<DurableBackgroundJobDiagnosticItem> jobs =
            await this.backgroundJobRepository.ListDiagnosticsAsync(
                new DurableBackgroundJobDiagnosticQuery(
                    Kind: RatingRankingRebuildScopeJob.Kind,
                    Limit: 100),
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
            RankingPublicationPointer? pointer = await this.snapshotRepository.GetPointerAsync(
                scope.Key,
                cancellationToken);
            RankingSnapshotHeader? header = await this.snapshotRepository.GetCurrentHeaderAsync(
                scope.Key,
                scope.MethodologyVersion,
                cancellationToken);
            RatingRankingSourceRevision? sourceRevision =
                await this.sourceRevisionRepository.GetAsync(scope.Key, cancellationToken);
            DurableBackgroundJobDiagnosticItem? latestJob = jobs
                .Where(job => string.Equals(
                    job.NaturalKey,
                    RatingRankingRebuildScopeJob.BuildNaturalKey(scope.Key),
                    StringComparison.Ordinal))
                .OrderByDescending(static job => job.UpdatedAtUtc)
                .FirstOrDefault();
            long resolvedSourceRevision = sourceRevision?.Revision ?? 0;
            bool isOutstanding = pointer is null
                || pointer.MethodologyVersion != scope.MethodologyVersion
                || pointer.HighestPublishedSourceRevision < resolvedSourceRevision;
            long? durationMilliseconds = header?.PublishedAtUtc is DateTime publishedAtUtc
                ? Math.Max(0, checked((long)(publishedAtUtc - header.GeneratedAtUtc).TotalMilliseconds))
                : null;
            RatingRankingPolicyEvaluationPlan evaluation =
                await this.policyEvaluationBuilder.EvaluateAsync(scope, policy, cancellationToken);
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

            if (evaluation.IsSourceTruncated)
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

            int remainingContributorCount = policy.EligibleMinUniqueContributors
                - evidence.UniqueContributorCount;
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
                evidence.UniqueContributorCount,
                policy.EligibleMinUniqueContributors,
                remainingContributorCount));
        }
    }

    private DateTime GetUtcNow()
    {
        return this.timeProvider.GetUtcNow().UtcDateTime;
    }
}
