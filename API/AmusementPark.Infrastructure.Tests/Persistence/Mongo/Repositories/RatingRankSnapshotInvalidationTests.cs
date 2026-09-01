using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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

    [Fact]
    public void BuildObservedRankingStateFilter_ShouldFenceEveryScopeRelevantParkItemField()
    {
        ParkItemDocument document = new ParkItemDocument
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Demo Ride",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            IsVisible = true,
            AttractionDetails = new AttractionDetailsDocument
            {
                Status = ParkItemStatusNormalizer.Operating,
            },
        };

        IBsonSerializer<ParkItemDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkItemDocument>();
        RenderArgs<ParkItemDocument> arguments =
            new RenderArgs<ParkItemDocument>(serializer, BsonSerializer.SerializerRegistry);
        BsonDocument filter = ParkItemRepository.BuildObservedRankingStateFilter(document)
            .Render(arguments);

        Assert.Equal(document.Id, filter["_id"].AsString);
        Assert.Equal(document.ParkId, filter["parkId"].AsString);
        Assert.Equal(document.Name, filter["name"].AsString);
        Assert.Equal(document.Category.ToString(), filter["category"].AsString);
        Assert.Equal(document.Type.ToString(), filter["type"].AsString);
        Assert.True(filter["isVisible"].AsBoolean);
        Assert.Equal(
            ParkItemStatusNormalizer.Operating,
            filter["attractionDetails.status"].AsString);
    }

    [Fact]
    public void BuildObservedRankingStateFilter_ShouldFenceEveryScopeRelevantParkField()
    {
        ParkDocument document = new ParkDocument
        {
            Id = "park-1",
            Name = "Demo Park",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };

        IBsonSerializer<ParkDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkDocument>();
        RenderArgs<ParkDocument> arguments =
            new RenderArgs<ParkDocument>(serializer, BsonSerializer.SerializerRegistry);
        BsonDocument filter = ParkRepository.BuildObservedRankingStateFilter(document)
            .Render(arguments);

        Assert.Equal(document.Id, filter["_id"].AsString);
        Assert.Equal(document.Name, filter["name"].AsString);
        Assert.True(filter["isVisible"].AsBoolean);
        Assert.Equal(document.Status.ToString(), filter["status"].AsString);
    }

    [Fact]
    public void MatchesBulkAdministrationTarget_WhenConcurrentParkStateDiffers_ShouldRequireRetry()
    {
        ParkDocument concurrent = new ParkDocument
        {
            Id = "park-1",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        bool result = ParkRepository.MatchesBulkAdministrationTarget(
            concurrent,
            isVisible: true,
            AdminReviewStatus.Validated);

        Assert.False(result);
    }

    [Fact]
    public void MatchesBulkFieldsTarget_WhenEveryRequestedValueIsApplied_ShouldFinishRetryLoop()
    {
        ParkItemDocument updated = new ParkItemDocument
        {
            Id = "item-1",
            ZoneId = "zone-2",
            Category = ParkItemCategory.Show,
            Type = ParkItemType.DarkRide,
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            AttractionDetails = new AttractionDetailsDocument
            {
                ManufacturerId = "manufacturer-2",
            },
        };

        bool result = ParkItemRepository.MatchesBulkFieldsTarget(
            updated,
            updateZone: true,
            "zone-2",
            ParkItemCategory.Show,
            ParkItemType.DarkRide,
            updateManufacturer: true,
            "manufacturer-2",
            isVisible: true,
            AdminReviewStatus.Validated);

        Assert.True(result);
    }

    [Fact]
    public void BuildFencedParkItemReplacement_WhenDocumentExists_ShouldUseObservedRankingState()
    {
        ParkItemDocument previous = new ParkItemDocument
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Demo Ride",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            IsVisible = false,
            AttractionDetails = new AttractionDetailsDocument
            {
                Status = ParkItemStatusNormalizer.Operating,
            },
        };
        ParkItemDocument replacement = new ParkItemDocument
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Imported Ride",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            IsVisible = false,
        };

        ReplaceOneModel<ParkItemDocument> write =
            CaptainCoasterDataSourceProvider.BuildFencedParkItemReplacement(replacement, previous);
        IBsonSerializer<ParkItemDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkItemDocument>();
        RenderArgs<ParkItemDocument> arguments =
            new RenderArgs<ParkItemDocument>(serializer, BsonSerializer.SerializerRegistry);
        BsonDocument filter = write.Filter.Render(arguments);

        Assert.False(write.IsUpsert);
        Assert.Equal(previous.Id, filter["_id"].AsString);
        Assert.Equal(previous.ParkId, filter["parkId"].AsString);
        Assert.Equal(previous.Category.ToString(), filter["category"].AsString);
        Assert.Equal(previous.Type.ToString(), filter["type"].AsString);
        Assert.Equal(previous.Name, filter["name"].AsString);
        Assert.False(filter["isVisible"].AsBoolean);
        Assert.Equal(
            ParkItemStatusNormalizer.Operating,
            filter["attractionDetails.status"].AsString);
    }

    [Fact]
    public void BuildFencedParkReplacement_WhenDocumentIsNew_ShouldKeepUpsertById()
    {
        ParkDocument replacement = new ParkDocument
        {
            Id = "park-1",
            Name = "Imported Park",
            IsVisible = false,
        };

        ReplaceOneModel<ParkDocument> write =
            CaptainCoasterDataSourceProvider.BuildFencedParkReplacement(replacement, null);
        IBsonSerializer<ParkDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkDocument>();
        RenderArgs<ParkDocument> arguments =
            new RenderArgs<ParkDocument>(serializer, BsonSerializer.SerializerRegistry);
        BsonDocument filter = write.Filter.Render(arguments);

        Assert.True(write.IsUpsert);
        Assert.Single(filter);
        Assert.Equal(replacement.Id, filter["_id"].AsString);
    }

    [Fact]
    public void DocumentsAreEquivalent_WhenConcurrentRankingFieldDiffers_ShouldKeepReplacementPending()
    {
        ParkItemDocument replacement = new ParkItemDocument
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Imported Ride",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            IsVisible = false,
        };
        ParkItemDocument concurrent = new ParkItemDocument
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Imported Ride",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            IsVisible = true,
        };

        bool result = CaptainCoasterDataSourceProvider.DocumentsAreEquivalent(
            concurrent,
            replacement);

        Assert.False(result);
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
            Array.Empty<RatingRankingMutationLease>());
        Mock<IRatingRankingSourceChangeCoordinator> coordinator =
            new Mock<IRatingRankingSourceChangeCoordinator>(MockBehavior.Strict);
        coordinator
            .Setup(value => value.PrepareParkChangesAsync(
                It.IsAny<IReadOnlyCollection<Park>>(),
                It.IsAny<IReadOnlyCollection<Park>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparation);
        coordinator
            .Setup(value => value.CompleteMutationAsync(
                preparation,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        return coordinator;
    }

    private static Mock<IRatingRankingSourceChangeCoordinator> CreateParkItemSourceChangeCoordinator()
    {
        RatingRankingMutationPreparation preparation = new RatingRankingMutationPreparation(
            Array.Empty<RatingRankingMutationLease>());
        Mock<IRatingRankingSourceChangeCoordinator> coordinator =
            new Mock<IRatingRankingSourceChangeCoordinator>(MockBehavior.Strict);
        coordinator
            .Setup(value => value.PrepareParkItemChangesAsync(
                It.IsAny<IReadOnlyCollection<ParkItem>>(),
                It.IsAny<IReadOnlyCollection<ParkItem>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparation);
        coordinator
            .Setup(value => value.CompleteMutationAsync(
                preparation,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        return coordinator;
    }
}
