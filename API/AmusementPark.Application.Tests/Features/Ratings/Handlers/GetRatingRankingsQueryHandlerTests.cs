using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class GetRatingRankingsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCanonicalSnapshotsAreEnabled_ShouldHydrateOnlyRequestedPublishedEntries()
    {
        DateTime generatedAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);
        RatingPublishedRankingSnapshot snapshot = CreatePublishedParkSnapshot(generatedAtUtc);
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetVisibleParkRankingSnapshotSourceBatchAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-03", "park-04" })),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RatingRankingSourceBatch(
                CreateParkSources().Where(source => source.ParkId is "park-03" or "park-04").ToList(),
                IsTruncated: false));
        Mock<IRatingEvidenceReader> ratingEvidenceReader = new Mock<IRatingEvidenceReader>(MockBehavior.Strict);
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider
            .Setup(provider => provider.GetCanonicalSnapshotAsync(
                RatingTargetType.Park,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        Mock<IRatingRankingFeatureFlags> featureFlags = new Mock<IRatingRankingFeatureFlags>(MockBehavior.Strict);
        featureFlags.SetupGet(flags => flags.EligibilityEnabled).Returns(true);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        GetRatingRankingsQueryHandler handler = new GetRatingRankingsQueryHandler(
            ratingRepository.Object,
            ratingEvidenceReader.Object,
            new PagedQueryValidator(),
            featureFlags.Object,
            new CanonicalParkRatingRankingReader(
                rankProvider.Object,
                ratingRepository.Object,
                parkRepository.Object));

        ApplicationResult<PagedResult<ParkRatingRankingResult>> result = await handler.HandleAsync(
            new GetRatingRankingsQuery(null, new PagedQuery(2, 2), null));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.TotalItems);
        Assert.Collection(
            result.Value.Items,
            first =>
            {
                Assert.Equal("park-03", first.ParkId);
                Assert.Equal(3, first.Rank);
                Assert.Equal(generatedAtUtc, first.GeneratedAtUtc);
                Assert.Equal(RankingEvidenceLevel.Eligible, first.Evidence?.Level);
            },
            second =>
            {
                Assert.Equal("park-04", second.ParkId);
                Assert.Equal(4, second.Rank);
            });
        ratingRepository.VerifyAll();
        ratingEvidenceReader.VerifyNoOtherCalls();
        rankProvider.VerifyAll();
        parkRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenCanonicalSnapshotIsUnavailable_ShouldReturnNoWeakLegacyRanking()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        Mock<IRatingEvidenceReader> ratingEvidenceReader = new Mock<IRatingEvidenceReader>(MockBehavior.Strict);
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider
            .Setup(provider => provider.GetCanonicalSnapshotAsync(
                RatingTargetType.Park,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RatingPublishedRankingSnapshot?)null);
        Mock<IRatingRankingFeatureFlags> featureFlags = new Mock<IRatingRankingFeatureFlags>(MockBehavior.Strict);
        featureFlags.SetupGet(flags => flags.EligibilityEnabled).Returns(true);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        GetRatingRankingsQueryHandler handler = new GetRatingRankingsQueryHandler(
            ratingRepository.Object,
            ratingEvidenceReader.Object,
            new PagedQueryValidator(),
            featureFlags.Object,
            new CanonicalParkRatingRankingReader(
                rankProvider.Object,
                ratingRepository.Object,
                parkRepository.Object));

        ApplicationResult<PagedResult<ParkRatingRankingResult>> result = await handler.HandleAsync(
            new GetRatingRankingsQuery(null, new PagedQuery(1, 20), null));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalItems);
        ratingRepository.VerifyNoOtherCalls();
        ratingEvidenceReader.VerifyNoOtherCalls();
        rankProvider.VerifyAll();
        parkRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenCanonicalEntryCannotBeHydrated_ShouldWithholdTheWholePage()
    {
        RatingPublishedRankingSnapshot snapshot = CreatePublishedParkSnapshot(
            new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc));
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetVisibleParkRankingSnapshotSourceBatchAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-01", "park-02" })),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RatingRankingSourceBatch(
                CreateParkSources().Where(static source => source.ParkId == "park-01").ToList(),
                IsTruncated: false));
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider
            .Setup(provider => provider.GetCanonicalSnapshotAsync(
                RatingTargetType.Park,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        Mock<IRatingRankingFeatureFlags> featureFlags = new Mock<IRatingRankingFeatureFlags>(MockBehavior.Strict);
        featureFlags.SetupGet(flags => flags.EligibilityEnabled).Returns(true);
        Mock<IRatingEvidenceReader> ratingEvidenceReader = new Mock<IRatingEvidenceReader>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        GetRatingRankingsQueryHandler handler = new GetRatingRankingsQueryHandler(
            ratingRepository.Object,
            ratingEvidenceReader.Object,
            new PagedQueryValidator(),
            featureFlags.Object,
            new CanonicalParkRatingRankingReader(
                rankProvider.Object,
                ratingRepository.Object,
                parkRepository.Object));

        ApplicationResult<PagedResult<ParkRatingRankingResult>> result = await handler.HandleAsync(
            new GetRatingRankingsQuery(null, new PagedQuery(1, 2), null));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalItems);
        ratingRepository.VerifyAll();
        ratingEvidenceReader.VerifyNoOtherCalls();
        rankProvider.VerifyAll();
        parkRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenCategoryFilterIsNotCanonical_ShouldSortWithoutPublishingARank()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetVisibleRankingSourcesAsync(
                ParkItemCategory.Attraction,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RatingRankingSourceBatch(
                new[]
                {
                    CreateParkItemSource(
                        "coaster-1",
                        "Taron",
                        "park-1",
                        "Phantasialand",
                        ParkItemType.RollerCoaster,
                        4.225d),
                },
                IsTruncated: true));
        Mock<IRatingEvidenceReader> ratingEvidenceReader = new Mock<IRatingEvidenceReader>(MockBehavior.Strict);
        Mock<IRatingRankingFeatureFlags> featureFlags = new Mock<IRatingRankingFeatureFlags>(MockBehavior.Strict);
        featureFlags.SetupGet(flags => flags.EligibilityEnabled).Returns(true);
        Mock<ICanonicalParkRatingRankingReader> canonicalReader =
            new Mock<ICanonicalParkRatingRankingReader>(MockBehavior.Strict);
        GetRatingRankingsQueryHandler handler = new GetRatingRankingsQueryHandler(
            ratingRepository.Object,
            ratingEvidenceReader.Object,
            new PagedQueryValidator(),
            featureFlags.Object,
            canonicalReader.Object);

        ApplicationResult<PagedResult<ParkRatingRankingResult>> result = await handler.HandleAsync(
            new GetRatingRankingsQuery(
                ParkItemCategory.Attraction,
                new PagedQuery(1, 20),
                null));

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(result.Value!.Items).Rank);
        ratingRepository.VerifyAll();
        ratingEvidenceReader.VerifyNoOtherCalls();
        canonicalReader.VerifyNoOtherCalls();
    }

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
            new PagedQueryValidator(),
            DisabledFeatureFlags.Instance,
            Mock.Of<ICanonicalParkRatingRankingReader>());

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
            new PagedQueryValidator(),
            DisabledFeatureFlags.Instance,
            Mock.Of<ICanonicalParkRatingRankingReader>());

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
            new PagedQueryValidator(),
            DisabledFeatureFlags.Instance,
            Mock.Of<ICanonicalParkRatingRankingReader>());

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
            new PagedQueryValidator(),
            DisabledFeatureFlags.Instance,
            Mock.Of<ICanonicalParkItemRatingRankingReader>());

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

    private static RatingPublishedRankingSnapshot CreatePublishedParkSnapshot(DateTime generatedAtUtc)
    {
        IReadOnlyCollection<RankingSnapshotEntry> entries = Enumerable.Range(0, 5)
            .Select(index => new RankingSnapshotEntry(
                index + 1,
                index + 1,
                RatingTargetType.Park,
                $"park-{index + 1:00}",
                null,
                4.9d - (index * 0.1d),
                CreateParkEvidence()))
            .ToList();
        return new RatingPublishedRankingSnapshot(
            CanonicalRankingScopes.GlobalParks.Key,
            RankingSnapshotId.Parse("snapshot-public"),
            RankingEligibilityPolicy.InitialMethodologyVersion,
            7,
            1,
            generatedAtUtc,
            entries);
    }

    private static RankingEvidence CreateParkEvidence()
    {
        return new RankingEvidence(
            RankingEvidenceLevel.Eligible,
            true,
            12,
            12,
            12,
            0,
            0,
            0,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            null)
        {
            NextContributorThreshold = 30,
            IsSingleCategoryParkException = false,
            PublicItemCategoryCount = 1,
        };
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

    private sealed class DisabledFeatureFlags : IRatingRankingFeatureFlags
    {
        public static DisabledFeatureFlags Instance { get; } = new DisabledFeatureFlags();

        public bool EligibilityEnabled => false;

        private DisabledFeatureFlags()
        {
        }
    }
}
