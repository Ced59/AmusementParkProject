using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ImageRepositoryMutationPreconditionTests
{
    [Fact]
    public async Task SetCurrentIfUnchangedAsync_ShouldMutateOnlyInsideTheOwnerScopeLock()
    {
        bool lockActive = false;
        bool demotionCompleted = false;
        int targetWriteCount = 0;
        ImageDocument reservedDocument = new ImageDocument
        {
            Id = "avatar-1",
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-1",
            Category = ImageCategory.Avatar,
            IsCurrent = false,
            CurrentPromotionToken = "promotion-1",
            UpdatedAt = DateTime.UtcNow,
        };
        ImageDocument updatedDocument = new ImageDocument
        {
            Id = "avatar-1",
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-1",
            Category = ImageCategory.Avatar,
            IsCurrent = true,
        };
        Mock<IMongoCollection<ImageDocument>> collection =
            new Mock<IMongoCollection<ImageDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ImageDocument>>(),
                It.IsAny<UpdateDefinition<ImageDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ImageDocument, ImageDocument>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                Assert.True(lockActive);
                targetWriteCount++;
                if (targetWriteCount == 1)
                {
                    Assert.False(demotionCompleted);
                }
                else
                {
                    Assert.Equal(2, targetWriteCount);
                    Assert.True(demotionCompleted);
                }
            })
            .ReturnsAsync(() =>
                targetWriteCount == 1 ? reservedDocument : updatedDocument);
        collection.Setup(value => value.UpdateManyAsync(
                It.IsAny<FilterDefinition<ImageDocument>>(),
                It.IsAny<UpdateDefinition<ImageDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                Assert.True(lockActive);
                demotionCompleted = true;
            })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<ImageDocument>("images", null))
            .Returns(collection.Object);
        Mock<IImageCurrentMutationLock> mutationLock =
            new Mock<IImageCurrentMutationLock>(MockBehavior.Strict);
        mutationLock.Setup(value => value.ExecuteAsync(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                It.IsAny<Func<CancellationToken, Task<Image?>>>(),
                CancellationToken.None))
            .Returns(async (
                ImageOwnerType _,
                string _,
                ImageCategory _,
                Func<CancellationToken, Task<Image?>> operation,
                CancellationToken cancellationToken) =>
            {
                lockActive = true;
                try
                {
                    return await operation(cancellationToken);
                }
                finally
                {
                    lockActive = false;
                }
            });
        using MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        ImageRepository repository = new ImageRepository(
            database.Object,
            new MongoDbSettings { ImagesCollectionName = "images" },
            cache,
            mutationLock.Object);

        Image? result = await repository.SetCurrentIfUnchangedAsync(
            "avatar-1",
            new ImageMutationPrecondition(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                false),
            ImageOwnerType.User,
            "owner-1",
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(lockActive);
        Assert.Equal(2, targetWriteCount);
        collection.VerifyAll();
        database.VerifyAll();
        mutationLock.VerifyAll();
    }

    [Fact]
    public async Task SetCurrentIfUnchangedAsync_WhenDemotionTimesOut_ShouldReconcileBeforeReturning()
    {
        int demotionAttempts = 0;
        int targetWriteCount = 0;
        ImageDocument reservedDocument = new ImageDocument
        {
            Id = "avatar-1",
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-1",
            Category = ImageCategory.Avatar,
            IsCurrent = false,
            CurrentPromotionToken = "promotion-1",
            UpdatedAt = DateTime.UtcNow,
        };
        ImageDocument updatedDocument = new ImageDocument
        {
            Id = "avatar-1",
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-1",
            Category = ImageCategory.Avatar,
            IsCurrent = true,
        };
        Mock<IMongoCollection<ImageDocument>> collection =
            new Mock<IMongoCollection<ImageDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ImageDocument>>(),
                It.IsAny<UpdateDefinition<ImageDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ImageDocument, ImageDocument>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                targetWriteCount++;
                if (targetWriteCount == 1)
                {
                    Assert.Equal(0, demotionAttempts);
                }
                else
                {
                    Assert.Equal(2, targetWriteCount);
                    Assert.Equal(2, demotionAttempts);
                }
            })
            .ReturnsAsync(() =>
                targetWriteCount == 1 ? reservedDocument : updatedDocument);
        collection.Setup(value => value.UpdateManyAsync(
                It.IsAny<FilterDefinition<ImageDocument>>(),
                It.IsAny<UpdateDefinition<ImageDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                demotionAttempts++;
                return demotionAttempts == 1
                    ? Task.FromException<UpdateResult>(
                        new TimeoutException("Ambiguous MongoDB timeout."))
                    : Task.FromResult<UpdateResult>(
                        new UpdateResult.Acknowledged(1, 1, null));
            });
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<ImageDocument>("images", null))
            .Returns(collection.Object);
        Mock<IImageCurrentMutationLock> mutationLock =
            new Mock<IImageCurrentMutationLock>(MockBehavior.Strict);
        mutationLock.Setup(value => value.ExecuteAsync(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                It.IsAny<Func<CancellationToken, Task<Image?>>>(),
                CancellationToken.None))
            .Returns((
                ImageOwnerType _,
                string _,
                ImageCategory _,
                Func<CancellationToken, Task<Image?>> operation,
                CancellationToken _) => operation(CancellationToken.None));
        using MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        ImageRepository repository = new ImageRepository(
            database.Object,
            new MongoDbSettings { ImagesCollectionName = "images" },
            cache,
            mutationLock.Object);

        Image? result = await repository.SetCurrentIfUnchangedAsync(
            "avatar-1",
            new ImageMutationPrecondition(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                false),
            ImageOwnerType.User,
            "owner-1",
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, targetWriteCount);
        collection.Verify(value => value.UpdateManyAsync(
            It.IsAny<FilterDefinition<ImageDocument>>(),
            It.IsAny<UpdateDefinition<ImageDocument>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        collection.VerifyAll();
        database.VerifyAll();
        mutationLock.VerifyAll();
    }

    [Fact]
    public async Task SetCurrentIfUnchangedAsync_WhenLeaseExpiresAfterDemotion_ShouldNotPromoteTarget()
    {
        using CancellationTokenSource leaseCancellation = new CancellationTokenSource();
        int targetWriteCount = 0;
        ImageDocument reservedDocument = new ImageDocument
        {
            Id = "avatar-1",
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-1",
            Category = ImageCategory.Avatar,
            IsCurrent = false,
            CurrentPromotionToken = "promotion-1",
            UpdatedAt = DateTime.UtcNow,
        };
        Mock<IMongoCollection<ImageDocument>> collection =
            new Mock<IMongoCollection<ImageDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ImageDocument>>(),
                It.IsAny<UpdateDefinition<ImageDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ImageDocument, ImageDocument>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => targetWriteCount++)
            .ReturnsAsync(reservedDocument);
        collection.Setup(value => value.UpdateManyAsync(
                It.IsAny<FilterDefinition<ImageDocument>>(),
                It.IsAny<UpdateDefinition<ImageDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => leaseCancellation.Cancel())
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<ImageDocument>("images", null))
            .Returns(collection.Object);
        Mock<IImageCurrentMutationLock> mutationLock =
            new Mock<IImageCurrentMutationLock>(MockBehavior.Strict);
        mutationLock.Setup(value => value.ExecuteAsync(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                It.IsAny<Func<CancellationToken, Task<Image?>>>(),
                CancellationToken.None))
            .Returns((
                ImageOwnerType _,
                string _,
                ImageCategory _,
                Func<CancellationToken, Task<Image?>> operation,
                CancellationToken _) => operation(leaseCancellation.Token));
        using MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        ImageRepository repository = new ImageRepository(
            database.Object,
            new MongoDbSettings { ImagesCollectionName = "images" },
            cache,
            mutationLock.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.SetCurrentIfUnchangedAsync(
                "avatar-1",
                new ImageMutationPrecondition(
                    ImageOwnerType.User,
                    "owner-1",
                    ImageCategory.Avatar,
                    false),
                ImageOwnerType.User,
                "owner-1",
                CancellationToken.None));

        collection.Verify(value => value.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ImageDocument>>(),
            It.IsAny<UpdateDefinition<ImageDocument>>(),
            It.IsAny<FindOneAndUpdateOptions<ImageDocument, ImageDocument>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, targetWriteCount);
        collection.VerifyAll();
        database.VerifyAll();
        mutationLock.VerifyAll();
    }

    [Fact]
    public void BuildMutationPreconditionFilter_ShouldFenceTheObservedOwnershipScope()
    {
        DateTime updatedAtUtc = new DateTime(
            2026,
            9,
            6,
            17,
            30,
            0,
            DateTimeKind.Utc);
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildMutationPreconditionFilter(
                "avatar-1",
                new ImageMutationPrecondition(
                    ImageOwnerType.User,
                    "owner-before",
                    ImageCategory.Avatar,
                    true,
                    updatedAtUtc));
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments = new RenderArgs<ImageDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);

        BsonDocument rendered = filter.Render(arguments);

        Assert.Equal("avatar-1", rendered["_id"].AsString);
        Assert.Equal("User", rendered["ownerType"].AsString);
        Assert.Equal("owner-before", rendered["ownerId"].AsString);
        Assert.Equal("Avatar", rendered["category"].AsString);
        Assert.True(rendered["isCurrent"].AsBoolean);
        Assert.False(rendered["currentPromotionToken"]["$exists"].AsBoolean);
        Assert.Equal(updatedAtUtc, rendered["updatedAt"].ToUniversalTime());
    }

    [Fact]
    public void BuildUnreservedImageFilter_ShouldRejectAnActivePromotionReservation()
    {
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments = new RenderArgs<ImageDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);

        BsonDocument rendered = ImageRepository.BuildUnreservedImageFilter("avatar-1")
            .Render(arguments);

        Assert.Equal("avatar-1", rendered["_id"].AsString);
        Assert.False(rendered["currentPromotionToken"]["$exists"].AsBoolean);
    }

    [Fact]
    public void BuildPromotionReservationFilter_ShouldAllowReclaimingTheDestinationReservation()
    {
        DateTime updatedAtUtc = new DateTime(
            2026,
            9,
            6,
            20,
            0,
            0,
            DateTimeKind.Utc);
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildPromotionReservationFilter(
                "avatar-1",
                new ImageMutationPrecondition(
                    ImageOwnerType.Park,
                    "park-before",
                    ImageCategory.Avatar,
                    true,
                    updatedAtUtc),
                ImageOwnerType.User,
                "owner-1");
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments = new RenderArgs<ImageDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);

        BsonDocument rendered = filter.Render(arguments);
        BsonArray alternatives = rendered["$or"].AsBsonArray;
        BsonDocument expectedState = alternatives[0].AsBsonDocument;
        BsonDocument staleReservation = alternatives[1].AsBsonDocument;

        Assert.Equal("park-before", expectedState["ownerId"].AsString);
        Assert.False(expectedState["currentPromotionToken"]["$exists"].AsBoolean);
        Assert.Equal(updatedAtUtc, expectedState["updatedAt"].ToUniversalTime());
        Assert.Equal("avatar-1", staleReservation["_id"].AsString);
        Assert.Equal("User", staleReservation["ownerType"].AsString);
        Assert.Equal("owner-1", staleReservation["ownerId"].AsString);
        Assert.Equal("Avatar", staleReservation["category"].AsString);
        Assert.False(staleReservation["isCurrent"].AsBoolean);
        Assert.True(staleReservation["currentPromotionToken"]["$exists"].AsBoolean);
        Assert.False(staleReservation.Contains("updatedAt"));
    }

    [Fact]
    public void BuildPromotionReservationUpdate_ShouldKeepTheTargetNonCurrent()
    {
        DateTime updatedAtUtc = new DateTime(
            2026,
            9,
            6,
            19,
            0,
            0,
            DateTimeKind.Utc);
        UpdateDefinition<ImageDocument> update =
            ImageRepository.BuildPromotionReservationUpdate(
                ImageOwnerType.User,
                "owner-1",
                "promotion-1",
                updatedAtUtc);
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments = new RenderArgs<ImageDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);

        BsonDocument rendered = update.Render(arguments).AsBsonDocument["$set"].AsBsonDocument;

        Assert.Equal("User", rendered["ownerType"].AsString);
        Assert.Equal("owner-1", rendered["ownerId"].AsString);
        Assert.False(rendered["isCurrent"].AsBoolean);
        Assert.Equal("promotion-1", rendered["currentPromotionToken"].AsString);
        Assert.Equal(updatedAtUtc, rendered["updatedAt"].ToUniversalTime());
    }

    [Fact]
    public void BuildPromotionActivationFilter_ShouldUseTheReservationTokenInsteadOfMutableMetadataTimestamp()
    {
        ImageDocument reservation = new ImageDocument
        {
            Id = "avatar-1",
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-1",
            Category = ImageCategory.Avatar,
            IsCurrent = false,
            CurrentPromotionToken = "promotion-1",
            UpdatedAt = DateTime.UtcNow,
        };
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments = new RenderArgs<ImageDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);

        BsonDocument rendered = ImageRepository.BuildPromotionActivationFilter(reservation)
            .Render(arguments);

        Assert.Equal("avatar-1", rendered["_id"].AsString);
        Assert.Equal("promotion-1", rendered["currentPromotionToken"].AsString);
        Assert.False(rendered.Contains("updatedAt"));
    }

    [Fact]
    public void BuildPromotionActivationUpdate_ShouldMakeTheReservedTargetCurrent()
    {
        DateTime updatedAtUtc = new DateTime(
            2026,
            9,
            6,
            19,
            5,
            0,
            DateTimeKind.Utc);
        UpdateDefinition<ImageDocument> update =
            ImageRepository.BuildPromotionActivationUpdate(updatedAtUtc);
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments = new RenderArgs<ImageDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);

        BsonDocument rendered = update.Render(arguments).AsBsonDocument["$set"].AsBsonDocument;

        Assert.True(rendered["isCurrent"].AsBoolean);
        Assert.True(
            update.Render(arguments).AsBsonDocument["$unset"].AsBsonDocument
                .Contains("currentPromotionToken"));
        Assert.Equal(updatedAtUtc, rendered["updatedAt"].ToUniversalTime());
    }

    [Fact]
    public void BuildSafeLinkUpdate_WhenOwnerChanges_ShouldDemoteTheTransferredImage()
    {
        DateTime updatedAtUtc = new DateTime(
            2026,
            9,
            6,
            16,
            45,
            0,
            DateTimeKind.Utc);
        UpdateDefinition<ImageDocument> update = ImageRepository.BuildSafeLinkUpdate(
            new ImageMutationPrecondition(
                ImageOwnerType.User,
                "owner-before",
                ImageCategory.Avatar,
                true),
            ImageOwnerType.User,
            "owner-after",
            updatedAtUtc);
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments = new RenderArgs<ImageDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);

        BsonDocument rendered = update.Render(arguments).AsBsonDocument["$set"].AsBsonDocument;

        Assert.Equal("User", rendered["ownerType"].AsString);
        Assert.Equal("owner-after", rendered["ownerId"].AsString);
        Assert.False(rendered["isCurrent"].AsBoolean);
        Assert.Equal(updatedAtUtc, rendered["updatedAt"].ToUniversalTime());
    }

    [Fact]
    public void BuildSafeLinkUpdate_WhenOwnerIsUnchanged_ShouldPreserveCurrentState()
    {
        UpdateDefinition<ImageDocument> update = ImageRepository.BuildSafeLinkUpdate(
            new ImageMutationPrecondition(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                true),
            ImageOwnerType.User,
            "owner-1",
            DateTime.UtcNow);
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments = new RenderArgs<ImageDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);

        BsonDocument rendered = update.Render(arguments).AsBsonDocument["$set"].AsBsonDocument;

        Assert.True(rendered["isCurrent"].AsBoolean);
    }
}
