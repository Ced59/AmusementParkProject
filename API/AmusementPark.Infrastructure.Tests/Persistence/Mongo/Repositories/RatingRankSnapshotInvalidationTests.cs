using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class RatingRankSnapshotInvalidationTests
{
    [Fact]
    public async Task CreateParkAsync_WhenWriteSucceeds_ShouldInvalidateRatingRanks()
    {
        Mock<IMongoCollection<ParkDocument>> collection = new Mock<IMongoCollection<ParkDocument>>(MockBehavior.Strict);
        collection
            .Setup(value => value.InsertOneAsync(
                It.IsAny<ParkDocument>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IMongoDatabase> database = CreateDatabase("parks", collection.Object);
        Mock<IRatingRankSnapshotCache> snapshotCache = CreateSnapshotCache();
        Mock<IRatingRankingSourceChangeCoordinator> sourceChanges = CreateParkSourceChangeCoordinator();
        ParkRepository repository = new ParkRepository(
            database.Object,
            new MongoDbSettings { ParksCollectionName = "parks" },
            snapshotCache.Object,
            sourceChanges.Object);

        await repository.CreateAsync(
            new Park
            {
                Id = "park-1",
                Name = "Demo Park",
                IsVisible = true,
            },
            CancellationToken.None);

        collection.VerifyAll();
        snapshotCache.VerifyAll();
        sourceChanges.VerifyAll();
    }

    [Fact]
    public async Task CreateParkItemAsync_WhenWriteSucceeds_ShouldInvalidateRatingRanks()
    {
        Mock<IMongoCollection<ParkItemDocument>> collection = new Mock<IMongoCollection<ParkItemDocument>>(MockBehavior.Strict);
        collection
            .Setup(value => value.InsertOneAsync(
                It.IsAny<ParkItemDocument>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IMongoDatabase> database = CreateDatabase("parkItems", collection.Object);
        Mock<IRatingRankSnapshotCache> snapshotCache = CreateSnapshotCache();
        Mock<IRatingRankingSourceChangeCoordinator> sourceChanges = CreateParkItemSourceChangeCoordinator();
        ParkItemRepository repository = new ParkItemRepository(
            database.Object,
            new MongoDbSettings { ParkItemsCollectionName = "parkItems" },
            snapshotCache.Object,
            sourceChanges.Object);

        await repository.CreateAsync(
            new ParkItem
            {
                Id = "item-1",
                ParkId = "park-1",
                Name = "Demo Ride",
                Category = ParkItemCategory.Attraction,
                IsVisible = true,
            },
            CancellationToken.None);

        collection.VerifyAll();
        snapshotCache.VerifyAll();
        sourceChanges.VerifyAll();
    }

    private static Mock<IMongoDatabase> CreateDatabase<TDocument>(
        string collectionName,
        IMongoCollection<TDocument> collection)
    {
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database
            .Setup(value => value.GetCollection<TDocument>(collectionName, null))
            .Returns(collection);
        return database;
    }

    private static Mock<IRatingRankSnapshotCache> CreateSnapshotCache()
    {
        Mock<IRatingRankSnapshotCache> snapshotCache =
            new Mock<IRatingRankSnapshotCache>(MockBehavior.Strict);
        snapshotCache
            .Setup(value => value.Invalidate());
        return snapshotCache;
    }

    private static Mock<IRatingRankingSourceChangeCoordinator> CreateParkSourceChangeCoordinator()
    {
        RatingRankingMutationPreparation preparation = new RatingRankingMutationPreparation(
            Array.Empty<RatingRankingSourceRevision>());
        Mock<IRatingRankingSourceChangeCoordinator> coordinator =
            new Mock<IRatingRankingSourceChangeCoordinator>(MockBehavior.Strict);
        coordinator
            .Setup(value => value.PrepareParkChangesAsync(
                It.IsAny<IReadOnlyCollection<Park>>(),
                It.IsAny<IReadOnlyCollection<Park>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparation);
        coordinator
            .Setup(value => value.ScheduleRebuildsAsync(
                preparation,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return coordinator;
    }

    private static Mock<IRatingRankingSourceChangeCoordinator> CreateParkItemSourceChangeCoordinator()
    {
        RatingRankingMutationPreparation preparation = new RatingRankingMutationPreparation(
            Array.Empty<RatingRankingSourceRevision>());
        Mock<IRatingRankingSourceChangeCoordinator> coordinator =
            new Mock<IRatingRankingSourceChangeCoordinator>(MockBehavior.Strict);
        coordinator
            .Setup(value => value.PrepareParkItemChangesAsync(
                It.IsAny<IReadOnlyCollection<ParkItem>>(),
                It.IsAny<IReadOnlyCollection<ParkItem>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparation);
        coordinator
            .Setup(value => value.ScheduleRebuildsAsync(
                preparation,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return coordinator;
    }
}
