using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
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

public sealed class GetParkItemRatingRankingsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCanonicalSnapshotsAreEnabled_ShouldReturnPublishedRankAndTimestamp()
    {
        DateTime generatedAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);
        RatingPublishedRankingSnapshot snapshot = CreatePublishedItemSnapshot(generatedAtUtc);
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetAggregatesAsync(
                RatingTargetType.ParkItem,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "ride-2" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateAggregate("ride-2") });
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider
            .Setup(provider => provider.GetCanonicalSnapshotAsync(
                RatingTargetType.ParkItem,
                ParkItemCategory.Attraction,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "ride-2" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateParkItem("ride-2", ParkItemType.RollerCoaster) });
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park
                {
                    Id = "park-1",
                    Name = "Demo Park",
                    IsVisible = true,
                    Status = ParkStatus.Operating,
                },
            });
        GetParkItemRatingRankingsQueryHandler handler = new GetParkItemRatingRankingsQueryHandler(
            new PagedQueryValidator(),
            new CanonicalParkItemRatingRankingReader(
                rankProvider.Object,
                ratingRepository.Object,
                parkRepository.Object,
                parkItemRepository.Object));

        ApplicationResult<PagedResult<ParkItemRatingRankingResult>> result = await handler.HandleAsync(
            new GetParkItemRatingRankingsQuery(
                ParkItemCategory.Attraction,
                new PagedQuery(2, 1),
                null,
                null));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.TotalItems);
        ParkItemRatingRankingResult ranking = Assert.Single(result.Value.Items);
        Assert.Equal("ride-2", ranking.TargetId);
        Assert.Equal(2, ranking.Rank);
        Assert.Equal(4.7d, ranking.BayesianScore);
        Assert.Equal(generatedAtUtc, ranking.GeneratedAtUtc);
        Assert.Equal(RankingEvidenceLevel.Eligible, ranking.Evidence?.Level);
        ratingRepository.VerifyAll();
        rankProvider.VerifyAll();
        parkItemRepository.VerifyAll();
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenTypeNarrowsCanonicalScope_ShouldKeepOrderWithoutPublishingSubsetRanks()
    {
        RatingPublishedRankingSnapshot snapshot = CreatePublishedItemSnapshot(
            new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc));
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetAggregatesAsync(
                RatingTargetType.ParkItem,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "ride-1", "ride-3" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateAggregate("ride-1"), CreateAggregate("ride-3") });
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider
            .Setup(provider => provider.GetCanonicalSnapshotAsync(
                RatingTargetType.ParkItem,
                ParkItemCategory.Attraction,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "ride-1", "ride-2", "ride-3" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreateParkItem("ride-1", ParkItemType.FlatRide),
                CreateParkItem("ride-2", ParkItemType.RollerCoaster),
                CreateParkItem("ride-3", ParkItemType.FlatRide),
            });
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1", "park-1", "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park
                {
                    Id = "park-1",
                    Name = "Demo Park",
                    IsVisible = true,
                    Status = ParkStatus.Operating,
                },
            });
        CanonicalParkItemRatingRankingReader canonicalReader =
            new CanonicalParkItemRatingRankingReader(
                rankProvider.Object,
                ratingRepository.Object,
                parkRepository.Object,
                parkItemRepository.Object);
        GetParkItemRatingRankingsQueryHandler handler = new GetParkItemRatingRankingsQueryHandler(
            new PagedQueryValidator(),
            canonicalReader);

        ApplicationResult<PagedResult<ParkItemRatingRankingResult>> result = await handler.HandleAsync(
            new GetParkItemRatingRankingsQuery(
                ParkItemCategory.Attraction,
                new PagedQuery(1, 20),
                null,
                ParkItemType.FlatRide));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalItems);
        Assert.Equal(new[] { "ride-1", "ride-3" }, result.Value.Items.Select(static item => item.TargetId));
        Assert.All(result.Value.Items, static item => Assert.Null(item.Rank));
        ratingRepository.VerifyAll();
        rankProvider.VerifyAll();
        parkItemRepository.VerifyAll();
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenCanonicalEntryCannotBeHydrated_ShouldWithholdTheWholePage()
    {
        RatingPublishedRankingSnapshot snapshot = CreatePublishedItemSnapshot(
            new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc));
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetAggregatesAsync(
                RatingTargetType.ParkItem,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "ride-1", "ride-2" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateAggregate("ride-1") });
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider
            .Setup(provider => provider.GetCanonicalSnapshotAsync(
                RatingTargetType.ParkItem,
                ParkItemCategory.Attraction,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "ride-1", "ride-2" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreateParkItem("ride-1", ParkItemType.RollerCoaster),
                CreateParkItem("ride-2", ParkItemType.RollerCoaster),
            });
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1", "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park
                {
                    Id = "park-1",
                    Name = "Demo Park",
                    IsVisible = true,
                    Status = ParkStatus.Operating,
                },
            });
        GetParkItemRatingRankingsQueryHandler handler = new GetParkItemRatingRankingsQueryHandler(
            new PagedQueryValidator(),
            new CanonicalParkItemRatingRankingReader(
                rankProvider.Object,
                ratingRepository.Object,
                parkRepository.Object,
                parkItemRepository.Object));

        ApplicationResult<PagedResult<ParkItemRatingRankingResult>> result = await handler.HandleAsync(
            new GetParkItemRatingRankingsQuery(
                ParkItemCategory.Attraction,
                new PagedQuery(1, 2),
                null,
                null));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalItems);
        ratingRepository.VerifyAll();
        rankProvider.VerifyAll();
        parkItemRepository.VerifyAll();
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSearchHasSeveralPages_ShouldReturnRequestedPage()
    {
        RatingPublishedRankingSnapshot snapshot = CreatePublishedItemSnapshot(
            new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc));
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratingRepository
            .Setup(repository => repository.GetAggregatesAsync(
                RatingTargetType.ParkItem,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "ride-2" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateAggregate("ride-2") });
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider
            .Setup(provider => provider.GetCanonicalSnapshotAsync(
                RatingTargetType.ParkItem,
                ParkItemCategory.Attraction,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids =>
                    ids.SequenceEqual(new[] { "ride-1", "ride-2", "ride-3" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreateParkItem("ride-1", ParkItemType.RollerCoaster),
                CreateParkItem("ride-2", ParkItemType.RollerCoaster),
                CreateParkItem("ride-3", ParkItemType.RollerCoaster),
            });
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(
                    new[] { "park-1", "park-1", "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park
                {
                    Id = "park-1",
                    Name = "Demo Park",
                    IsVisible = true,
                    Status = ParkStatus.Operating,
                },
            });
        GetParkItemRatingRankingsQueryHandler handler = new GetParkItemRatingRankingsQueryHandler(
            new PagedQueryValidator(),
            new CanonicalParkItemRatingRankingReader(
                rankProvider.Object,
                ratingRepository.Object,
                parkRepository.Object,
                parkItemRepository.Object));

        ApplicationResult<PagedResult<ParkItemRatingRankingResult>> result = await handler.HandleAsync(
            new GetParkItemRatingRankingsQuery(
                ParkItemCategory.Attraction,
                new PagedQuery(2, 1),
                " ride "));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Page);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(3, result.Value.TotalPages);
        ParkItemRatingRankingResult ranking = Assert.Single(result.Value.Items);
        Assert.Equal(2, ranking.Rank);
        Assert.Equal("ride-2", ranking.TargetName);
        Assert.Equal(12, ranking.RatingObservationCount);
        Assert.Equal(12, ranking.UniqueContributorCount);
        Assert.Equal(RankingEvidenceLevel.Eligible, ranking.Evidence?.Level);
        Assert.Equal("ratings-2026-01", ranking.MethodologyVersion?.ToString());
        ratingRepository.VerifyAll();
        rankProvider.VerifyAll();
        parkItemRepository.VerifyAll();
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenCanonicalSnapshotIsUnavailable_ShouldReturnNoLegacyRanking()
    {
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider
            .Setup(provider => provider.GetCanonicalSnapshotAsync(
                RatingTargetType.ParkItem,
                ParkItemCategory.Attraction,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RatingPublishedRankingSnapshot?)null);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        GetParkItemRatingRankingsQueryHandler handler = new GetParkItemRatingRankingsQueryHandler(
            new PagedQueryValidator(),
            new CanonicalParkItemRatingRankingReader(
                rankProvider.Object,
                ratingRepository.Object,
                parkRepository.Object,
                parkItemRepository.Object));

        ApplicationResult<PagedResult<ParkItemRatingRankingResult>> result = await handler.HandleAsync(
            new GetParkItemRatingRankingsQuery(
                ParkItemCategory.Attraction,
                new PagedQuery(1, 20),
                null,
                null));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalItems);
        ratingRepository.VerifyNoOtherCalls();
        rankProvider.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        parkRepository.VerifyNoOtherCalls();
    }

    private static RatingPublishedRankingSnapshot CreatePublishedItemSnapshot(DateTime generatedAtUtc)
    {
        RankingScopeDefinition scope = CanonicalRankingScopes.PublicItemCategories.Single(
            static definition => definition.Filter.ParkItemCategory == ParkItemCategory.Attraction);
        IReadOnlyCollection<RankingSnapshotEntry> entries = Enumerable.Range(1, 3)
            .Select(position => new RankingSnapshotEntry(
                position,
                position,
                RatingTargetType.ParkItem,
                $"ride-{position}",
                ParkItemCategory.Attraction,
                4.9d - (position * 0.1d),
                RankingEligibilityPolicy.Initial.EvaluateSimpleTarget(
                    new SimpleRankingEvidenceInput(12, 12, true, false, true))))
            .ToList();
        return new RatingPublishedRankingSnapshot(
            scope.Key,
            RankingSnapshotId.Parse("snapshot-items"),
            scope.MethodologyVersion,
            7,
            1,
            generatedAtUtc,
            entries);
    }

    private static RatingAggregate CreateAggregate(string targetId)
    {
        return new RatingAggregate
        {
            TargetType = RatingTargetType.ParkItem,
            TargetId = targetId,
            ParkId = "park-1",
            ParkItemCategory = ParkItemCategory.Attraction,
            ParkItemType = ParkItemType.RollerCoaster,
            RatingCount = 12,
            UniqueContributorCount = 12,
            RatingSum = 57,
            AverageRating = 4.75,
            BayesianScore = 4.7,
            MutationVersion = 1,
            CalculatedVersion = 1,
            SourceIntegrityIsValid = true,
        };
    }

    private static ParkItem CreateParkItem(string targetId, ParkItemType type)
    {
        return new ParkItem
        {
            Id = targetId,
            ParkId = "park-1",
            Name = targetId,
            Category = ParkItemCategory.Attraction,
            Type = type,
            IsVisible = true,
            AttractionDetails = new AttractionDetails
            {
                Status = ParkItemStatusNormalizer.Operating,
            },
        };
    }

}
