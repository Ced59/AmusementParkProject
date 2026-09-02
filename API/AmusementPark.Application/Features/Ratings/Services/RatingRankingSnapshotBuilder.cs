using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RatingRankingSnapshotBuilder :
    IRatingRankingSnapshotBuilder,
    IRatingRankingPolicyEvaluationBuilder
{
    private readonly IRatingRepository ratingRepository;
    private readonly IRatingEvidenceReader ratingEvidenceReader;

    public RatingRankingSnapshotBuilder(
        IRatingRepository ratingRepository,
        IRatingEvidenceReader ratingEvidenceReader)
    {
        this.ratingRepository = ratingRepository;
        this.ratingEvidenceReader = ratingEvidenceReader;
    }

    public Task<RatingRankingSnapshotBuildPlan> BuildAsync(
        RankingScopeDefinition scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return scope.TargetFamily switch
        {
            RankingTargetFamily.Parks => this.BuildParkScopeAsync(scope, cancellationToken),
            RankingTargetFamily.ParkItems => this.BuildParkItemScopeAsync(scope, cancellationToken),
            _ => throw new InvalidOperationException("The ranking scope target family is not supported."),
        };
    }

    public Task<RatingRankingPolicyEvaluationPlan> EvaluateAsync(
        RankingScopeDefinition scope,
        RankingEligibilityPolicy eligibilityPolicy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(eligibilityPolicy);
        return scope.TargetFamily switch
        {
            RankingTargetFamily.Parks => this.EvaluateParkScopeAsync(
                scope,
                eligibilityPolicy,
                cancellationToken),
            RankingTargetFamily.ParkItems => this.EvaluateParkItemScopeAsync(
                scope,
                eligibilityPolicy,
                cancellationToken),
            _ => throw new InvalidOperationException("The ranking scope target family is not supported."),
        };
    }

    private async Task<RatingRankingSnapshotBuildPlan> BuildParkScopeAsync(
        RankingScopeDefinition scope,
        CancellationToken cancellationToken)
    {
        RatingRankingParkCandidateBatch parkCandidateBatch =
            await this.ratingRepository.GetVisibleParkRankingSnapshotCandidateBatchAsync(
                RankingSnapshotHeader.MaximumCandidateEntryCount,
                cancellationToken);
        if (parkCandidateBatch.IsTruncated)
        {
            return new RatingRankingSnapshotBuildPlan(
                parkCandidateBatch.ParkIds.Count,
                Array.Empty<RankingSnapshotEntry>(),
                true);
        }

        List<ParkRankingSnapshotCandidate> candidates = new List<ParkRankingSnapshotCandidate>();
        for (int offset = 0;
             offset < parkCandidateBatch.ParkIds.Count;
             offset += RatingRankingSnapshotBuildLimits.ParkCandidateBatchSize)
        {
            IReadOnlyCollection<string> parkIds = parkCandidateBatch.ParkIds
                .Skip(offset)
                .Take(RatingRankingSnapshotBuildLimits.ParkCandidateBatchSize)
                .ToArray();
            RatingRankingSourceBatch sourceBatch =
                await this.ratingRepository.GetVisibleParkRankingSnapshotSourceBatchAsync(
                    parkIds,
                    RatingRankingSnapshotBuildLimits.MaximumSourceComponentCountPerParkBatch,
                    cancellationToken);
            if (sourceBatch.IsTruncated)
            {
                return new RatingRankingSnapshotBuildPlan(
                    parkCandidateBatch.ParkIds.Count,
                    Array.Empty<RankingSnapshotEntry>(),
                    true);
            }

            IReadOnlyCollection<RatingRankingItemResult> sources = sourceBatch.Sources;
            IReadOnlyCollection<ParkRatingRankingResult> rankings =
                RatingRankingFactory.BuildParkRankings(sources);
            ParkRankingEvidenceFactsBatch evidenceFacts =
                await this.ratingEvidenceReader.ReadParkRankingFactsAsync(
                    sources.Select(static source => new RatingEvidenceTarget(
                            source.TargetType,
                            source.TargetId,
                            source.ParkId))
                        .Distinct()
                        .ToList(),
                    cancellationToken);
            candidates.AddRange(RatingRankingFactory.BuildParkSnapshotCandidates(
                rankings,
                sources,
                evidenceFacts));
        }

        IReadOnlyCollection<ParkRankingSnapshotCandidate> orderedCandidates =
            RatingRankingFactory.OrderParkSnapshotCandidates(candidates);
        IReadOnlyList<ParkRankingSnapshotCandidate> eligibleCandidates = orderedCandidates
            .Where(static candidate => candidate.Evidence?.IsEligibleForMainRanking == true)
            .ToArray();
        IReadOnlyList<CompetitionRankAssignment> rankAssignments =
            CompetitionRankCalculator.AssignOrderedRanks(
                scope,
                eligibleCandidates.Select(static candidate => candidate.Ranking.Score));
        List<RankingSnapshotEntry> entries = new List<RankingSnapshotEntry>();
        for (int index = 0; index < eligibleCandidates.Count; index++)
        {
            ParkRankingSnapshotCandidate candidate = eligibleCandidates[index];
            RankingEvidence evidence = candidate.Evidence!;
            CompetitionRankAssignment assignment = rankAssignments[index];
            entries.Add(new RankingSnapshotEntry(
                assignment.Position,
                assignment.Rank,
                RatingTargetType.Park,
                candidate.Ranking.ParkId,
                null,
                candidate.Ranking.Score,
                evidence));
        }

        return new RatingRankingSnapshotBuildPlan(orderedCandidates.Count, entries, false);
    }

    private async Task<RatingRankingSnapshotBuildPlan> BuildParkItemScopeAsync(
        RankingScopeDefinition scope,
        CancellationToken cancellationToken)
    {
        ParkItemCategory category = scope.Filter.ParkItemCategory
            ?? throw new InvalidOperationException("A park-item ranking scope requires a category.");
        RatingRankingSourceBatch sourceBatch =
            await this.ratingRepository.GetVisibleParkItemRankingSourceBatchAsync(
                category,
                RankingSnapshotHeader.MaximumCandidateEntryCount,
                cancellationToken);
        IReadOnlyCollection<RatingRankingItemResult> sources = sourceBatch.Sources;
        IReadOnlyCollection<ParkItemRatingRankingResult> rankings =
            RatingRankingFactory.BuildParkItemRankings(sources);
        if (sourceBatch.IsTruncated)
        {
            return new RatingRankingSnapshotBuildPlan(
                rankings.Count,
                Array.Empty<RankingSnapshotEntry>(),
                true);
        }

        IReadOnlyCollection<RatingAggregateSourceFact> sourceFacts =
            await this.ratingEvidenceReader.ReadAggregateSourceFactsAsync(
                sources.Select(static source => new RatingAggregateSourceTarget(
                        source.TargetType,
                        source.TargetId))
                    .Distinct()
                    .ToList(),
                cancellationToken);
        IReadOnlyCollection<ParkItemRankingSnapshotCandidate> candidates =
            RatingRankingFactory.BuildParkItemSnapshotCandidates(rankings, sources, sourceFacts);
        IReadOnlyList<ParkItemRankingSnapshotCandidate> eligibleCandidates = candidates
            .Where(static candidate => candidate.Evidence?.IsEligibleForMainRanking == true)
            .ToArray();
        IReadOnlyList<CompetitionRankAssignment> rankAssignments =
            CompetitionRankCalculator.AssignOrderedRanks(
                scope,
                eligibleCandidates.Select(static candidate => candidate.Ranking.BayesianScore));
        List<RankingSnapshotEntry> entries = new List<RankingSnapshotEntry>();
        for (int index = 0; index < eligibleCandidates.Count; index++)
        {
            ParkItemRankingSnapshotCandidate candidate = eligibleCandidates[index];
            RankingEvidence evidence = candidate.Evidence!;
            CompetitionRankAssignment assignment = rankAssignments[index];
            entries.Add(new RankingSnapshotEntry(
                assignment.Position,
                assignment.Rank,
                RatingTargetType.ParkItem,
                candidate.Ranking.TargetId,
                candidate.Ranking.ParkItemCategory,
                candidate.Ranking.BayesianScore,
                evidence));
        }

        return new RatingRankingSnapshotBuildPlan(rankings.Count, entries, false);
    }

    private async Task<RatingRankingPolicyEvaluationPlan> EvaluateParkScopeAsync(
        RankingScopeDefinition scope,
        RankingEligibilityPolicy eligibilityPolicy,
        CancellationToken cancellationToken)
    {
        RatingRankingParkCandidateBatch parkCandidateBatch =
            await this.ratingRepository.GetVisibleParkRankingSnapshotCandidateBatchAsync(
                RankingSnapshotHeader.MaximumCandidateEntryCount,
                cancellationToken);
        if (parkCandidateBatch.IsTruncated)
        {
            return new RatingRankingPolicyEvaluationPlan(
                parkCandidateBatch.ParkIds.Count,
                Array.Empty<RatingRankingPolicyEvaluationEntry>(),
                true);
        }

        List<ParkRankingSnapshotCandidate> candidates = new List<ParkRankingSnapshotCandidate>();
        for (int offset = 0;
             offset < parkCandidateBatch.ParkIds.Count;
             offset += RatingRankingSnapshotBuildLimits.ParkCandidateBatchSize)
        {
            IReadOnlyCollection<string> parkIds = parkCandidateBatch.ParkIds
                .Skip(offset)
                .Take(RatingRankingSnapshotBuildLimits.ParkCandidateBatchSize)
                .ToArray();
            RatingRankingSourceBatch sourceBatch =
                await this.ratingRepository.GetVisibleParkRankingSnapshotSourceBatchAsync(
                    parkIds,
                    RatingRankingSnapshotBuildLimits.MaximumSourceComponentCountPerParkBatch,
                    cancellationToken);
            if (sourceBatch.IsTruncated)
            {
                return new RatingRankingPolicyEvaluationPlan(
                    parkCandidateBatch.ParkIds.Count,
                    Array.Empty<RatingRankingPolicyEvaluationEntry>(),
                    true);
            }

            IReadOnlyCollection<RatingRankingItemResult> sources = sourceBatch.Sources;
            IReadOnlyCollection<ParkRatingRankingResult> rankings =
                RatingRankingFactory.BuildParkRankings(sources);
            ParkRankingEvidenceFactsBatch evidenceFacts =
                await this.ratingEvidenceReader.ReadParkRankingFactsAsync(
                    sources.Select(static source => new RatingEvidenceTarget(
                            source.TargetType,
                            source.TargetId,
                            source.ParkId))
                        .Distinct()
                        .ToList(),
                    cancellationToken);
            candidates.AddRange(RatingRankingFactory.BuildParkSnapshotCandidates(
                rankings,
                sources,
                evidenceFacts,
                eligibilityPolicy: eligibilityPolicy));
        }

        IReadOnlyCollection<ParkRankingSnapshotCandidate> orderedCandidates =
            RatingRankingFactory.OrderParkSnapshotCandidates(candidates);
        IReadOnlyCollection<RatingRankingPolicyEvaluationEntry> entries = orderedCandidates
            .Select(static candidate => new RatingRankingPolicyEvaluationEntry(
                RatingTargetType.Park,
                candidate.Ranking.ParkId,
                candidate.Ranking.ParkName,
                null,
                candidate.Ranking.Score,
                candidate.Evidence,
                candidate.ItemComponent))
            .ToArray();
        return new RatingRankingPolicyEvaluationPlan(entries.Count, entries, false);
    }

    private async Task<RatingRankingPolicyEvaluationPlan> EvaluateParkItemScopeAsync(
        RankingScopeDefinition scope,
        RankingEligibilityPolicy eligibilityPolicy,
        CancellationToken cancellationToken)
    {
        ParkItemCategory category = scope.Filter.ParkItemCategory
            ?? throw new InvalidOperationException("A park-item ranking scope requires a category.");
        RatingRankingSourceBatch sourceBatch =
            await this.ratingRepository.GetVisibleParkItemRankingSourceBatchAsync(
                category,
                RankingSnapshotHeader.MaximumCandidateEntryCount,
                cancellationToken);
        IReadOnlyCollection<RatingRankingItemResult> sources = sourceBatch.Sources;
        IReadOnlyCollection<ParkItemRatingRankingResult> rankings =
            RatingRankingFactory.BuildParkItemRankings(sources);
        if (sourceBatch.IsTruncated)
        {
            return new RatingRankingPolicyEvaluationPlan(
                rankings.Count,
                Array.Empty<RatingRankingPolicyEvaluationEntry>(),
                true);
        }

        IReadOnlyCollection<RatingAggregateSourceFact> sourceFacts =
            await this.ratingEvidenceReader.ReadAggregateSourceFactsAsync(
                sources.Select(static source => new RatingAggregateSourceTarget(
                        source.TargetType,
                        source.TargetId))
                    .Distinct()
                    .ToList(),
                cancellationToken);
        IReadOnlyCollection<ParkItemRankingSnapshotCandidate> candidates =
            RatingRankingFactory.BuildParkItemSnapshotCandidates(
                rankings,
                sources,
                sourceFacts,
                eligibilityPolicy);
        IReadOnlyCollection<RatingRankingPolicyEvaluationEntry> entries = candidates
            .Select(static candidate => new RatingRankingPolicyEvaluationEntry(
                RatingTargetType.ParkItem,
                candidate.Ranking.TargetId,
                candidate.Ranking.TargetName,
                candidate.Ranking.ParkItemCategory,
                candidate.Ranking.BayesianScore,
                candidate.Evidence,
                null))
            .ToArray();
        return new RatingRankingPolicyEvaluationPlan(entries.Count, entries, false);
    }
}
