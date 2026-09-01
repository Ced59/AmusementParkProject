using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RatingRankingSnapshotBuilder : IRatingRankingSnapshotBuilder
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

    private async Task<RatingRankingSnapshotBuildPlan> BuildParkScopeAsync(
        RankingScopeDefinition scope,
        CancellationToken cancellationToken)
    {
        RatingRankingSourceBatch sourceBatch = await this.ratingRepository.GetVisibleRankingSourcesAsync(
            null,
            RankingSnapshotHeader.MaximumCandidateEntryCount,
            cancellationToken);
        IReadOnlyCollection<RatingRankingItemResult> sources = sourceBatch.Sources;
        IReadOnlyCollection<ParkRatingRankingResult> rankings = RatingRankingFactory.BuildParkRankings(sources);
        if (sourceBatch.IsTruncated
            || rankings.Count > RankingSnapshotHeader.MaximumCandidateEntryCount)
        {
            return new RatingRankingSnapshotBuildPlan(
                rankings.Count,
                Array.Empty<RankingSnapshotEntry>(),
                true);
        }

        ParkRankingEvidenceFactsBatch evidenceFacts = await this.ratingEvidenceReader.ReadParkRankingFactsAsync(
            sources.Select(static source => new RatingEvidenceTarget(
                    source.TargetType,
                    source.TargetId,
                    source.ParkId))
                .Distinct()
                .ToList(),
            cancellationToken);
        IReadOnlyCollection<ParkRankingSnapshotCandidate> candidates =
            RatingRankingFactory.BuildParkSnapshotCandidates(rankings, sources, evidenceFacts);
        List<RankingSnapshotEntry> entries = new List<RankingSnapshotEntry>();
        int position = 0;
        int rank = 0;
        double? rankAnchorScore = null;
        foreach (ParkRankingSnapshotCandidate candidate in candidates)
        {
            RankingEvidence? evidence = candidate.Evidence;
            if (evidence is null || !evidence.IsEligibleForMainRanking)
            {
                continue;
            }

            position++;
            ResolveRank(scope, candidate.Ranking.Score, position, ref rank, ref rankAnchorScore);
            entries.Add(new RankingSnapshotEntry(
                position,
                rank,
                RatingTargetType.Park,
                candidate.Ranking.ParkId,
                null,
                candidate.Ranking.Score,
                evidence));
        }

        return new RatingRankingSnapshotBuildPlan(rankings.Count, entries, false);
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
        List<RankingSnapshotEntry> entries = new List<RankingSnapshotEntry>();
        int position = 0;
        int rank = 0;
        double? rankAnchorScore = null;
        foreach (ParkItemRankingSnapshotCandidate candidate in candidates)
        {
            RankingEvidence? evidence = candidate.Evidence;
            if (evidence is null || !evidence.IsEligibleForMainRanking)
            {
                continue;
            }

            position++;
            ResolveRank(scope, candidate.Ranking.BayesianScore, position, ref rank, ref rankAnchorScore);
            entries.Add(new RankingSnapshotEntry(
                position,
                rank,
                RatingTargetType.ParkItem,
                candidate.Ranking.TargetId,
                candidate.Ranking.ParkItemCategory,
                candidate.Ranking.BayesianScore,
                evidence));
        }

        return new RatingRankingSnapshotBuildPlan(rankings.Count, entries, false);
    }

    private static void ResolveRank(
        RankingScopeDefinition scope,
        double score,
        int position,
        ref int rank,
        ref double? rankAnchorScore)
    {
        if (!rankAnchorScore.HasValue || !scope.AreScoresTied(rankAnchorScore.Value, score))
        {
            rank = position;
            rankAnchorScore = score;
        }
    }
}
