using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class GetRatingRankingsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParkSearchMatches_ShouldReturnFiveRankingsAroundMatch()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetVisibleRankingSourcesAsync(null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RatingRankingSourceBatch(CreateParkSources(), IsTruncated: false));
        Mock<IRatingEvidenceReader> ratingEvidenceReader = new Mock<IRatingEvidenceReader>(MockBehavior.Strict);
        ratingEvidenceReader
            .Setup(reader => reader.ReadParkRankingFactsAsync(
                It.Is<IReadOnlyCollection<RatingEvidenceTarget>>(targets => targets.Count == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDirectOnlyEvidenceFacts());

        GetRatingRankingsQueryHandler handler = new GetRatingRankingsQueryHandler(
            ratingRepository.Object,
            ratingEvidenceReader.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<ParkRatingRankingResult>> result = await handler.HandleAsync(
            new GetRatingRankingsQuery(null, new PagedQuery(1, 20), "Park 08"));

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.Items.Count);
        Assert.Equal(3, result.Value.Items.First().Rank);
        Assert.Contains(result.Value.Items, static item => item.ParkName == "Park 08");
        Assert.Equal(12, result.Value.Items.Last().Rank);
        Assert.All(result.Value.Items, static item =>
        {
            Assert.Equal(RankingEvidenceLevel.Eligible, item.Evidence?.Level);
            Assert.Equal(10, item.UniqueContributorCount);
            Assert.Equal(10, item.RatingObservationCount);
        });
        ratingRepository.VerifyAll();
        ratingEvidenceReader.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenVisitorsOverlapAcrossParkAndItems_ShouldUseUnionAndPublicCoverage()
    {
        IReadOnlyCollection<RatingRankingItemResult> sources = CreateComposedParkSources();
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetVisibleRankingSourcesAsync(null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RatingRankingSourceBatch(sources, IsTruncated: false));
        Mock<IRatingEvidenceReader> ratingEvidenceReader = new Mock<IRatingEvidenceReader>(MockBehavior.Strict);
        ratingEvidenceReader
            .Setup(reader => reader.ReadParkRankingFactsAsync(
                It.Is<IReadOnlyCollection<RatingEvidenceTarget>>(targets => targets.Count == 6),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkRankingEvidenceFactsBatch(
                new[]
                {
                    new ParkRankingContributorFacts(
                        "park-composed",
                        UniqueContributorCount: 15,
                        RatingObservationCount: 60,
                        DirectParkContributorCount: 10,
                        ItemContributorCount: 12),
                },
                new[]
                {
                    new PublicParkItemEvidenceFact("park-composed", "item-1", ParkItemCategory.Attraction),
                    new PublicParkItemEvidenceFact("park-composed", "item-2", ParkItemCategory.Attraction),
                    new PublicParkItemEvidenceFact("park-composed", "item-3", ParkItemCategory.Attraction),
                    new PublicParkItemEvidenceFact("park-composed", "item-4", ParkItemCategory.Restaurant),
                    new PublicParkItemEvidenceFact("park-composed", "item-5", ParkItemCategory.Restaurant),
                }));
        GetRatingRankingsQueryHandler handler = new GetRatingRankingsQueryHandler(
            ratingRepository.Object,
            ratingEvidenceReader.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<ParkRatingRankingResult>> result = await handler.HandleAsync(
            new GetRatingRankingsQuery(null, new PagedQuery(1, 20), null));

        Assert.True(result.IsSuccess);
        ParkRatingRankingResult ranking = Assert.Single(result.Value!.Items);
        Assert.Equal(60, ranking.RatingCount);
        Assert.Equal(60, ranking.RatingObservationCount);
        Assert.Equal(15, ranking.UniqueContributorCount);
        Assert.Equal(10, ranking.Evidence?.DirectParkContributorCount);
        Assert.Equal(12, ranking.Evidence?.ItemContributorCount);
        Assert.Equal(5, ranking.Evidence?.EligibleItemCount);
        Assert.Equal(2, ranking.Evidence?.EligibleCategoryCount);
        Assert.Equal(RankingEvidenceLevel.Eligible, ranking.Evidence?.Level);
        Assert.True(ranking.Evidence?.IsEligibleForMainRanking);
        ratingRepository.VerifyAll();
        ratingEvidenceReader.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenRankingSourcesAreTruncated_ShouldWithholdEvidence()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetVisibleRankingSourcesAsync(
                null,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RatingRankingSourceBatch(CreateParkSources(), IsTruncated: true));
        Mock<IRatingEvidenceReader> ratingEvidenceReader = new Mock<IRatingEvidenceReader>(MockBehavior.Strict);
        GetRatingRankingsQueryHandler handler = new GetRatingRankingsQueryHandler(
            ratingRepository.Object,
            ratingEvidenceReader.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<ParkRatingRankingResult>> result = await handler.HandleAsync(
            new GetRatingRankingsQuery(null, new PagedQuery(1, 20), null));

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.Items);
        Assert.All(result.Value.Items, static ranking => Assert.Null(ranking.Evidence));
        ratingRepository.VerifyAll();
        ratingEvidenceReader.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenAttractionTypeIsSelected_ShouldRankMatchingItemsAcrossParks()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetVisibleParkItemRankingSourcesAsync(
                ParkItemCategory.Attraction,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreateParkItemSource("flat-1", "Talocan", "park-1", "Phantasialand", ParkItemType.FlatRide, 4.1),
                CreateParkItemSource("coaster-1", "Taron", "park-1", "Phantasialand", ParkItemType.RollerCoaster, 4.225),
                CreateParkItemSource("flat-2", "Sledge Hammer", "park-2", "Bobbejaanland", ParkItemType.FlatRide, 4.2),
            });
        Mock<IRatingEvidenceReader> ratingEvidenceReader = new Mock<IRatingEvidenceReader>(MockBehavior.Strict);
        ratingEvidenceReader
            .Setup(reader => reader.ReadAggregateSourceFactsAsync(
                It.Is<IReadOnlyCollection<RatingAggregateSourceTarget>>(targets => targets.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new RatingAggregateSourceFact(
                    RatingTargetType.ParkItem,
                    "flat-1",
                    UniqueContributorCount: 10,
                    RatingObservationCount: 10,
                    RatingSum: 47d),
                new RatingAggregateSourceFact(
                    RatingTargetType.ParkItem,
                    "flat-2",
                    UniqueContributorCount: 10,
                    RatingObservationCount: 10,
                    RatingSum: 49d),
            });
        GetParkItemRatingRankingsQueryHandler handler = new GetParkItemRatingRankingsQueryHandler(
            ratingRepository.Object,
            ratingEvidenceReader.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<ParkItemRatingRankingResult>> result = await handler.HandleAsync(
            new GetParkItemRatingRankingsQuery(
                ParkItemCategory.Attraction,
                new PagedQuery(1, 20),
                null,
                ParkItemType.FlatRide));

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value!.Items,
            first =>
            {
                Assert.Equal(1, first.Rank);
                Assert.Equal("Sledge Hammer", first.TargetName);
                Assert.Equal("Bobbejaanland", first.ParkName);
                Assert.Equal(RankingEvidenceLevel.Eligible, first.Evidence?.Level);
            },
            second =>
            {
                Assert.Equal(2, second.Rank);
                Assert.Equal("Talocan", second.TargetName);
                Assert.Equal("Phantasialand", second.ParkName);
                Assert.Equal(RankingEvidenceLevel.Eligible, second.Evidence?.Level);
            });
        ratingRepository.VerifyAll();
        ratingEvidenceReader.VerifyAll();
    }

    private static IReadOnlyCollection<RatingRankingItemResult> CreateParkSources()
    {
        List<RatingRankingItemResult> sources = new List<RatingRankingItemResult>();
        for (int index = 1; index <= 12; index += 1)
        {
            double score = 5d - (index * 0.1d);
            sources.Add(new RatingRankingItemResult(
                RatingTargetType.Park,
                $"park-{index:00}",
                $"Park {index:00}",
                $"park-{index:00}",
                $"Park {index:00}",
                null,
                null,
                10,
                score * 10,
                score,
                score)
            {
                UniqueContributorCount = 10,
                AggregateIntegrityIsValid = true,
            });
        }

        return sources;
    }

    private static ParkRankingEvidenceFactsBatch CreateDirectOnlyEvidenceFacts()
    {
        IReadOnlyCollection<ParkRankingContributorFacts> contributors = Enumerable.Range(1, 12)
            .Select(index => new ParkRankingContributorFacts(
                $"park-{index:00}",
                UniqueContributorCount: 10,
                RatingObservationCount: 10,
                DirectParkContributorCount: 10,
                ItemContributorCount: 0))
            .ToList();

        return new ParkRankingEvidenceFactsBatch(
            contributors,
            Array.Empty<PublicParkItemEvidenceFact>());
    }

    private static IReadOnlyCollection<RatingRankingItemResult> CreateComposedParkSources()
    {
        List<RatingRankingItemResult> sources = new List<RatingRankingItemResult>
        {
            new RatingRankingItemResult(
                RatingTargetType.Park,
                "park-composed",
                "Composed Park",
                "park-composed",
                "Composed Park",
                null,
                null,
                10,
                45,
                4.5,
                4.2)
            {
                UniqueContributorCount = 10,
                AggregateIntegrityIsValid = true,
            },
        };
        for (int index = 1; index <= 5; index += 1)
        {
            ParkItemCategory category = index <= 3
                ? ParkItemCategory.Attraction
                : ParkItemCategory.Restaurant;
            sources.Add(new RatingRankingItemResult(
                RatingTargetType.ParkItem,
                $"item-{index}",
                $"Item {index}",
                "park-composed",
                "Composed Park",
                category,
                category == ParkItemCategory.Attraction ? ParkItemType.RollerCoaster : null,
                10,
                45,
                4.5,
                4.1)
            {
                UniqueContributorCount = 10,
                AggregateIntegrityIsValid = true,
            });
        }

        return sources;
    }

    private static RatingRankingItemResult CreateParkItemSource(
        string targetId,
        string targetName,
        string parkId,
        string parkName,
        ParkItemType parkItemType,
        double bayesianScore)
    {
        double ratingSum = (bayesianScore * 20d) - 35d;
        return new RatingRankingItemResult(
            RatingTargetType.ParkItem,
            targetId,
            targetName,
            parkId,
            parkName,
            ParkItemCategory.Attraction,
            parkItemType,
            10,
            ratingSum,
            ratingSum / 10d,
            bayesianScore)
        {
            UniqueContributorCount = 10,
            AggregateIntegrityIsValid = true,
        };
    }
}
