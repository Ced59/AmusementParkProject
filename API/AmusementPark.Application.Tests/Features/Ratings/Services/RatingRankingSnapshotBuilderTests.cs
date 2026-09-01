using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingRankingSnapshotBuilderTests
{
    [Fact]
    public async Task BuildAsync_WhenParkItemsHaveMixedEvidence_ShouldKeepOnlyEligibleEntriesAndRecomputeCompetitionRanks()
    {
        RankingScopeDefinition scope = ResolveAttractionScope();
        IReadOnlyCollection<RatingRankingItemResult> sources = new[]
        {
            CreateSource("item-ineligible", 3.7, 2),
            CreateSource("item-1", 4.2, 10),
            CreateSource("item-2", 4.19995, 10),
            CreateSource("item-3", 4.0, 10),
        };
        Mock<IRatingRepository> repository = new Mock<IRatingRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetVisibleParkItemRankingSourceBatchAsync(
                ParkItemCategory.Attraction,
                RankingSnapshotHeader.MaximumCandidateEntryCount,
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceBatch(sources, false));
        Mock<IRatingEvidenceReader> evidenceReader =
            new Mock<IRatingEvidenceReader>(MockBehavior.Strict);
        evidenceReader
            .Setup(reader => reader.ReadAggregateSourceFactsAsync(
                It.Is<IReadOnlyCollection<RatingAggregateSourceTarget>>(targets => targets.Count == 4),
                CancellationToken.None))
            .ReturnsAsync(sources.Select(static source => new RatingAggregateSourceFact(
                    source.TargetType,
                    source.TargetId,
                    source.UniqueContributorCount!.Value,
                    source.RatingCount,
                    source.RatingSum))
                .ToArray());
        RatingRankingSnapshotBuilder builder = new RatingRankingSnapshotBuilder(
            repository.Object,
            evidenceReader.Object);

        RatingRankingSnapshotBuildPlan plan = await builder.BuildAsync(
            scope,
            CancellationToken.None);

        Assert.False(plan.IsSourceTruncated);
        Assert.Equal(4, plan.TotalEntryCount);
        Assert.Collection(
            plan.EligibleEntries,
            first =>
            {
                Assert.Equal("item-1", first.TargetId);
                Assert.Equal(1, first.Position);
                Assert.Equal(1, first.Rank);
            },
            second =>
            {
                Assert.Equal("item-2", second.TargetId);
                Assert.Equal(2, second.Position);
                Assert.Equal(1, second.Rank);
            },
            third =>
            {
                Assert.Equal("item-3", third.TargetId);
                Assert.Equal(3, third.Position);
                Assert.Equal(3, third.Rank);
            });
        Assert.All(plan.EligibleEntries, entry =>
        {
            Assert.Equal(ParkItemCategory.Attraction, entry.ParkItemCategory);
            Assert.True(entry.Evidence.IsEligibleForMainRanking);
            Assert.Equal(scope.MethodologyVersion, entry.Evidence.MethodologyVersion);
        });
        repository.VerifyAll();
        evidenceReader.VerifyAll();
    }

    [Fact]
    public async Task BuildAsync_WhenBoundedSourceReadIsTruncated_ShouldWithholdEntriesWithoutReadingEvidence()
    {
        RankingScopeDefinition scope = ResolveAttractionScope();
        IReadOnlyCollection<RatingRankingItemResult> sources = new[]
        {
            CreateSource("item-1", 4.2, 10),
        };
        Mock<IRatingRepository> repository = new Mock<IRatingRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetVisibleParkItemRankingSourceBatchAsync(
                ParkItemCategory.Attraction,
                RankingSnapshotHeader.MaximumCandidateEntryCount,
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceBatch(sources, true));
        Mock<IRatingEvidenceReader> evidenceReader =
            new Mock<IRatingEvidenceReader>(MockBehavior.Strict);
        RatingRankingSnapshotBuilder builder = new RatingRankingSnapshotBuilder(
            repository.Object,
            evidenceReader.Object);

        RatingRankingSnapshotBuildPlan plan = await builder.BuildAsync(
            scope,
            CancellationToken.None);

        Assert.True(plan.IsSourceTruncated);
        Assert.Equal(1, plan.TotalEntryCount);
        Assert.Empty(plan.EligibleEntries);
        repository.VerifyAll();
        evidenceReader.VerifyNoOtherCalls();
    }

    private static RankingScopeDefinition ResolveAttractionScope()
    {
        return CanonicalRankingScopes.PublicItemCategories.Single(
            static scope => scope.Filter.ParkItemCategory == ParkItemCategory.Attraction);
    }

    private static RatingRankingItemResult CreateSource(
        string targetId,
        double bayesianScore,
        long contributorCount)
    {
        double ratingSum = (bayesianScore * (contributorCount + 10d)) - 35d;
        return new RatingRankingItemResult(
            RatingTargetType.ParkItem,
            targetId,
            targetId,
            "park-1",
            "Demo Park",
            ParkItemCategory.Attraction,
            ParkItemType.RollerCoaster,
            contributorCount,
            ratingSum,
            ratingSum / contributorCount,
            bayesianScore)
        {
            UniqueContributorCount = contributorCount,
            AggregateIntegrityIsValid = true,
        };
    }
}
