using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RatingRankingPolicyImpactPreviewer
{
    private const int MaximumChangedTargetSamples = 10;

    private readonly IRankingScopeRegistry scopeRegistry;
    private readonly IRankingSnapshotRepository snapshotRepository;
    private readonly IRatingRankingSourceRevisionRepository sourceRevisionRepository;
    private readonly IRatingRankingPolicyEvaluationBuilder policyEvaluationBuilder;
    private readonly TimeProvider timeProvider;

    public RatingRankingPolicyImpactPreviewer(
        IRankingScopeRegistry scopeRegistry,
        IRankingSnapshotRepository snapshotRepository,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository,
        IRatingRankingPolicyEvaluationBuilder policyEvaluationBuilder,
        TimeProvider? timeProvider = null)
    {
        this.scopeRegistry = scopeRegistry;
        this.snapshotRepository = snapshotRepository;
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.policyEvaluationBuilder = policyEvaluationBuilder;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<RatingRankingPolicyImpactResult> PreviewImpactAsync(
        RatingRankingPolicyCandidate candidate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        RankingEligibilityPolicy candidatePolicy = candidate.ToDomain();
        List<RatingRankingPolicyScopeImpactResult> scopeImpacts =
            new List<RatingRankingPolicyScopeImpactResult>();

        foreach (RankingScopeDefinition currentScope in this.scopeRegistry.Definitions
                     .OrderBy(static definition => definition.Key.Value, StringComparer.Ordinal))
        {
            RankingScopeDefinition candidateScope = CreateCandidateScope(currentScope, candidatePolicy);
            RatingRankingSourceRevision? sourceRevisionBeforeEvaluation =
                await this.sourceRevisionRepository.GetAsync(
                    currentScope.Key,
                    cancellationToken);
            RatingRankingPolicyEvaluationPlan evaluation =
                await this.policyEvaluationBuilder.EvaluateAsync(
                    currentScope,
                    candidatePolicy,
                    cancellationToken);
            CurrentRankingSnapshot currentSnapshot = await this.LoadCurrentSnapshotAsync(
                currentScope,
                sourceRevisionBeforeEvaluation,
                cancellationToken);
            RatingRankingSourceRevision? sourceRevisionAfterSnapshot =
                await this.sourceRevisionRepository.GetAsync(
                    currentScope.Key,
                    cancellationToken);
            if (!SourceRevisionsMatch(
                    sourceRevisionBeforeEvaluation,
                    sourceRevisionAfterSnapshot))
            {
                currentSnapshot = CurrentRankingSnapshot.Unavailable;
            }
            scopeImpacts.Add(BuildScopeImpact(
                currentScope,
                candidateScope,
                evaluation,
                currentSnapshot));
        }

        int comparedRankCount = scopeImpacts.Sum(static scope => scope.ComparedRankCount);
        long totalAbsoluteRankChange = scopeImpacts.Sum(
            static scope => scope.TotalAbsoluteRankChange);
        int[] maximumRankChanges = scopeImpacts
            .Where(static scope => scope.MaximumRankChange.HasValue)
            .Select(static scope => scope.MaximumRankChange!.Value)
            .ToArray();

        return new RatingRankingPolicyImpactResult(
            this.GetUtcNow(),
            candidate with { Version = candidatePolicy.Version.Value },
            scopeImpacts.Sum(static scope => scope.GainedEligibilityCount),
            scopeImpacts.Sum(static scope => scope.LostEligibilityCount),
            comparedRankCount,
            totalAbsoluteRankChange,
            comparedRankCount == 0
                ? null
                : Math.Round((double)totalAbsoluteRankChange / comparedRankCount, 2),
            maximumRankChanges.Length == 0 ? null : maximumRankChanges.Max(),
            scopeImpacts.Count(static scope =>
                scope.IsImpactAvailable && !scope.HasMinimumComparableEntries),
            scopeImpacts.Sum(static scope => scope.IncompleteParkCompositionCount),
            scopeImpacts.Sum(static scope => scope.EstimatedTargetCount),
            scopeImpacts.Sum(static scope => scope.EstimatedChunkCount),
            scopeImpacts.AsReadOnly());
    }

    private static RatingRankingPolicyScopeImpactResult BuildScopeImpact(
        RankingScopeDefinition currentScope,
        RankingScopeDefinition candidateScope,
        RatingRankingPolicyEvaluationPlan evaluation,
        CurrentRankingSnapshot currentSnapshot)
    {
        if (evaluation.IsSourceTruncated)
        {
            return new RatingRankingPolicyScopeImpactResult(
                currentScope.Key.Value,
                currentScope.TargetFamily,
                currentScope.Filter.ParkItemCategory,
                currentSnapshot.IsAvailable,
                false,
                true,
                currentSnapshot.Ranks.Count,
                0,
                0,
                0,
                0,
                0,
                null,
                null,
                false,
                0,
                evaluation.TotalEntryCount,
                0,
                Array.Empty<RatingRankingPolicyTargetChangeResult>(),
                Array.Empty<RatingRankingPolicyTargetChangeResult>());
        }

        IReadOnlyList<RatingRankingPolicyEvaluationEntry> eligibleEntries = evaluation.Entries
            .Where(static entry => entry.Evidence?.IsEligibleForMainRanking == true)
            .ToArray();
        IReadOnlyList<CompetitionRankAssignment> candidateAssignments =
            CompetitionRankCalculator.AssignOrderedRanks(
                candidateScope,
                eligibleEntries.Select(static entry => entry.Score));
        Dictionary<string, int> candidateRanks = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int index = 0; index < eligibleEntries.Count; index++)
        {
            candidateRanks.Add(eligibleEntries[index].TargetId, candidateAssignments[index].Rank);
        }

        Dictionary<string, RatingRankingPolicyEvaluationEntry> entriesById = evaluation.Entries
            .GroupBy(static entry => entry.TargetId, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
        IReadOnlyCollection<string> gainedIds = currentSnapshot.IsAvailable
            ? candidateRanks.Keys.Except(currentSnapshot.Ranks.Keys, StringComparer.Ordinal).ToArray()
            : Array.Empty<string>();
        IReadOnlyCollection<string> lostIds = currentSnapshot.IsAvailable
            ? currentSnapshot.Ranks.Keys.Except(candidateRanks.Keys, StringComparer.Ordinal).ToArray()
            : Array.Empty<string>();
        int[] rankChanges = currentSnapshot.IsAvailable
            ? candidateRanks
                .Where(entry => currentSnapshot.Ranks.TryGetValue(entry.Key, out int _))
                .Select(entry => Math.Abs(entry.Value - currentSnapshot.Ranks[entry.Key]))
                .ToArray()
            : Array.Empty<int>();
        int incompleteParkCompositionCount = currentScope.TargetFamily == RankingTargetFamily.Parks
            ? evaluation.Entries.Count(static entry =>
                entry.ParkItemComponent is { IsEligible: false })
            : 0;
        int estimatedChunkCount = eligibleEntries.Count == 0
            ? 0
            : ((eligibleEntries.Count - 1) / currentScope.PageSize) + 1;

        return new RatingRankingPolicyScopeImpactResult(
            currentScope.Key.Value,
            currentScope.TargetFamily,
            currentScope.Filter.ParkItemCategory,
            currentSnapshot.IsAvailable,
            true,
            false,
            currentSnapshot.Ranks.Count,
            eligibleEntries.Count,
            gainedIds.Count,
            lostIds.Count,
            rankChanges.Length,
            rankChanges.Sum(static change => (long)change),
            rankChanges.Length == 0 ? null : Math.Round(rankChanges.Average(), 2),
            rankChanges.Length == 0 ? null : rankChanges.Max(),
            eligibleEntries.Count >= candidateScope.MinimumEligibleEntries,
            incompleteParkCompositionCount,
            evaluation.TotalEntryCount,
            estimatedChunkCount,
            BuildTargetChanges(
                gainedIds,
                entriesById,
                currentSnapshot.Ranks,
                candidateRanks,
                ToTargetType(currentScope.TargetFamily)),
            BuildTargetChanges(
                lostIds,
                entriesById,
                currentSnapshot.Ranks,
                candidateRanks,
                ToTargetType(currentScope.TargetFamily)));
    }

    private static IReadOnlyCollection<RatingRankingPolicyTargetChangeResult> BuildTargetChanges(
        IReadOnlyCollection<string> targetIds,
        IReadOnlyDictionary<string, RatingRankingPolicyEvaluationEntry> entriesById,
        IReadOnlyDictionary<string, int> currentRanks,
        IReadOnlyDictionary<string, int> candidateRanks,
        RatingTargetType fallbackTargetType)
    {
        return targetIds
            .Select(targetId =>
            {
                entriesById.TryGetValue(targetId, out RatingRankingPolicyEvaluationEntry? entry);
                return new RatingRankingPolicyTargetChangeResult(
                    entry?.TargetType ?? fallbackTargetType,
                    targetId,
                    entry?.TargetName ?? targetId,
                    currentRanks.TryGetValue(targetId, out int previousRank) ? previousRank : null,
                    candidateRanks.TryGetValue(targetId, out int candidateRank) ? candidateRank : null);
            })
            .OrderBy(static target => target.TargetName, StringComparer.OrdinalIgnoreCase)
            .Take(MaximumChangedTargetSamples)
            .ToArray();
    }

    private static RatingTargetType ToTargetType(RankingTargetFamily targetFamily)
    {
        return targetFamily == RankingTargetFamily.Parks
            ? RatingTargetType.Park
            : RatingTargetType.ParkItem;
    }

    private async Task<CurrentRankingSnapshot> LoadCurrentSnapshotAsync(
        RankingScopeDefinition scope,
        RatingRankingSourceRevision? sourceRevision,
        CancellationToken cancellationToken)
    {
        if (sourceRevision is not null && !sourceRevision.IsRebuildable)
        {
            return CurrentRankingSnapshot.Unavailable;
        }

        long expectedSourceRevision = sourceRevision?.Revision ?? 0;

        RankingSnapshotHeader? header = await this.snapshotRepository.GetCurrentHeaderAsync(
            scope.Key,
            scope.MethodologyVersion,
            cancellationToken);
        if (header is null || header.SourceRevision != expectedSourceRevision)
        {
            return CurrentRankingSnapshot.Unavailable;
        }

        Dictionary<string, int> ranks = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int offset = 0; offset < header.EligibleEntryCount; offset += scope.PageSize)
        {
            RankingSnapshotPage? page = await this.snapshotRepository.GetCurrentPageAsync(
                scope.Key,
                scope.MethodologyVersion,
                offset,
                scope.PageSize,
                cancellationToken);
            if (page is null || page.Header.Id != header.Id)
            {
                return CurrentRankingSnapshot.Unavailable;
            }

            foreach (RankingSnapshotEntry entry in page.Entries)
            {
                if (!ranks.TryAdd(entry.TargetId, entry.Rank))
                {
                    return CurrentRankingSnapshot.Unavailable;
                }
            }
        }

        return new CurrentRankingSnapshot(true, ranks);
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

    private static RankingScopeDefinition CreateCandidateScope(
        RankingScopeDefinition currentScope,
        RankingEligibilityPolicy candidatePolicy)
    {
        return new RankingScopeDefinition(
            currentScope.Key,
            currentScope.TargetFamily,
            currentScope.Filter,
            currentScope.IsPublic,
            candidatePolicy.Version,
            candidatePolicy.MinimumEligibleEntriesPerRanking,
            currentScope.PageSize,
            candidatePolicy.ScoreTieEpsilon,
            currentScope.PublicationMode);
    }

    private DateTime GetUtcNow()
    {
        return this.timeProvider.GetUtcNow().UtcDateTime;
    }

    private sealed record CurrentRankingSnapshot(
        bool IsAvailable,
        IReadOnlyDictionary<string, int> Ranks)
    {
        public static CurrentRankingSnapshot Unavailable { get; } =
            new CurrentRankingSnapshot(false, new Dictionary<string, int>(StringComparer.Ordinal));
    }
}
