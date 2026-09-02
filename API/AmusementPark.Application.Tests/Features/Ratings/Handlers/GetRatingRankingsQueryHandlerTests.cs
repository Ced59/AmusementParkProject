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
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        GetRatingRankingsQueryHandler handler = new GetRatingRankingsQueryHandler(
            ratingRepository.Object,
            ratingEvidenceReader.Object,
            new PagedQueryValidator(),
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
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        GetRatingRankingsQueryHandler handler = new GetRatingRankingsQueryHandler(
            ratingRepository.Object,
            ratingEvidenceReader.Object,
            new PagedQueryValidator(),
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
        Mock<IRatingEvidenceReader> ratingEvidenceReader = new Mock<IRatingEvidenceReader>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        GetRatingRankingsQueryHandler handler = new GetRatingRankingsQueryHandler(
            ratingRepository.Object,
            ratingEvidenceReader.Object,
            new PagedQueryValidator(),
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
                    CreateParkItemSource(
                        "coaster-2",
                        "Untamed",
                        "park-2",
                        "Walibi Holland",
                        ParkItemType.RollerCoaster,
                        4.3d),
                },
                IsTruncated: true));
        Mock<IRatingEvidenceReader> ratingEvidenceReader = new Mock<IRatingEvidenceReader>(MockBehavior.Strict);
        Mock<ICanonicalParkRatingRankingReader> canonicalReader =
            new Mock<ICanonicalParkRatingRankingReader>(MockBehavior.Strict);
        GetRatingRankingsQueryHandler handler = new GetRatingRankingsQueryHandler(
            ratingRepository.Object,
            ratingEvidenceReader.Object,
            new PagedQueryValidator(),
            canonicalReader.Object);

        ApplicationResult<PagedResult<ParkRatingRankingResult>> result = await handler.HandleAsync(
            new GetRatingRankingsQuery(
                ParkItemCategory.Attraction,
                new PagedQuery(1, 20),
                null));

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value!.Items,
            first =>
            {
                Assert.Equal("Walibi Holland", first.ParkName);
                Assert.Null(first.Rank);
            },
            second =>
            {
                Assert.Equal("Phantasialand", second.ParkName);
                Assert.Null(second.Rank);
            });
        ratingRepository.VerifyAll();
        ratingEvidenceReader.VerifyNoOtherCalls();
        canonicalReader.VerifyNoOtherCalls();
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
