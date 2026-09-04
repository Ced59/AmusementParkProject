using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class GlobalRatingSuggestionPersistenceTests
{
    private static readonly DateTime RatingUpdatedAtUtc =
        new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildSources_SeparatesParkAndRideObservationsAndHonorsContentFence()
    {
        GlobalRatingSuggestionRatingSourceDocument[] ratings =
        {
            Rating(RatingTargetType.Park, "park-1", "park-1", 4.5d),
            Rating(RatingTargetType.ParkItem, "item-1", "park-1", 4d),
        };
        GlobalRatingSuggestionVisitSourceDocument[] visits =
        {
            new GlobalRatingSuggestionVisitSourceDocument
            {
                Id = "visit-1",
                ParkId = "park-1",
                ContentMutationFenceToken = 7,
                ContentMutationFenceStableToken = 7,
                ContentMutationFenceReady = true,
                AssessmentValueHalfSteps = 6,
                AssessmentUpdatedAtUtc = RatingUpdatedAtUtc.AddDays(1),
            },
        };
        GlobalRatingSuggestionOccurrenceSourceDocument[] occurrences =
        {
            Ride("visit-1", "park-1", "item-1", 7, 5),
            Ride("visit-1", "park-1", "item-1", 6, 3),
        };

        IReadOnlyCollection<GlobalRatingSuggestionSource> result =
            GlobalRatingSuggestionSourceReader.BuildSources(ratings, visits, occurrences);

        GlobalRatingSuggestionSource park = Assert.Single(
            result,
            static source => source.TargetType == RatingTargetType.Park);
        Assert.Equal(3d, Assert.Single(park.Observations).Value.DoubleValue);
        GlobalRatingSuggestionSource item = Assert.Single(
            result,
            static source => source.TargetType == RatingTargetType.ParkItem);
        Assert.Equal(2.5d, Assert.Single(item.Observations).Value.DoubleValue);
    }

    [Fact]
    public void BuildSources_SkipsCorruptCurrentRatingsAndInvalidObservations()
    {
        IReadOnlyCollection<GlobalRatingSuggestionSource> result =
            GlobalRatingSuggestionSourceReader.BuildSources(
                new[] { Rating(RatingTargetType.Park, "park-1", "park-1", 5.2d) },
                new[]
                {
                    new GlobalRatingSuggestionVisitSourceDocument
                    {
                        Id = "visit-1",
                        ParkId = "park-1",
                        AssessmentValueHalfSteps = 12,
                        AssessmentUpdatedAtUtc = RatingUpdatedAtUtc,
                    },
                },
                Array.Empty<GlobalRatingSuggestionOccurrenceSourceDocument>());

        Assert.Empty(result);
    }

    [Fact]
    public void RatingFilter_IncludesPersistedRatingsWithoutPlaceholderField()
    {
        FilterDefinition<UserRatingDocument> filter =
            GlobalRatingSuggestionSourceReader.BuildRatingFilter(" owner-1 ");

        Assert.Equal(
            new BsonDocument
            {
                { "userId", "owner-1" },
                { "isMutationPlaceholder", new BsonDocument("$ne", true) },
            },
            Render(filter));
    }

    [Fact]
    public void Indexes_EnforcePerUserTargetsAndBoundInteractionRetention()
    {
        CreateIndexModel<GlobalRatingSuggestionStateDocument> state = Assert.Single(
            GlobalRatingSuggestionMongoDefinitions.BuildStateIndexes());
        CreateIndexModel<GlobalRatingSuggestionPreferenceDocument> preference = Assert.Single(
            GlobalRatingSuggestionMongoDefinitions.BuildPreferenceIndexes());
        CreateIndexModel<GlobalRatingSuggestionInteractionDocument> ttl = Assert.Single(
            GlobalRatingSuggestionMongoDefinitions.BuildInteractionIndexes(),
            static index => index.Options.Name == "ratingSuggestionInteraction_ttl");

        Assert.True(state.Options.Unique);
        Assert.Equal(
            new BsonDocument { { "userId", 1 }, { "targetType", 1 }, { "targetId", 1 } },
            Render(state.Keys));
        Assert.True(preference.Options.Unique);
        Assert.Equal(TimeSpan.FromDays(400), ttl.Options.ExpireAfter);
    }

    [Fact]
    public void AnalyticsDocument_DoesNotPersistTargetOrExactRatingValues()
    {
        GlobalRatingSuggestionInteractionDocument document =
            new GlobalRatingSuggestionInteractionDocument
            {
                Id = "event-1",
                UserCohortKey = "hashed-user",
                TargetType = RatingTargetType.ParkItem,
                InteractionType = GlobalRatingSuggestionInteractionType.Accepted,
                OccurredAtUtc = RatingUpdatedAtUtc,
            };

        BsonDocument bson = document.ToBsonDocument();

        Assert.False(bson.Contains("userId"));
        Assert.False(bson.Contains("targetId"));
        Assert.False(bson.Contains("rating"));
        Assert.False(bson.Contains("value"));
    }

    [Fact]
    public void MongoSettings_UseDedicatedCollectionsByDefault()
    {
        MongoDbSettings settings = new MongoDbSettings();

        Assert.Equal(
            "global-rating-suggestion-states",
            settings.GlobalRatingSuggestionStatesCollectionName);
        Assert.Equal(
            "global-rating-suggestion-preferences",
            settings.GlobalRatingSuggestionPreferencesCollectionName);
        Assert.Equal(
            "global-rating-suggestion-interactions",
            settings.GlobalRatingSuggestionInteractionsCollectionName);
    }

    [Fact]
    public async Task TerminalTransition_WhenPresentationWasAlreadyResolved_DoesNotWriteAnalytics()
    {
        Mock<IMongoCollection<GlobalRatingSuggestionStateDocument>> states =
            new Mock<IMongoCollection<GlobalRatingSuggestionStateDocument>>(MockBehavior.Strict);
        states.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<GlobalRatingSuggestionStateDocument>>(),
                It.IsAny<UpdateDefinition<GlobalRatingSuggestionStateDocument>>(),
                It.Is<UpdateOptions>(static options => !options.IsUpsert),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));
        Mock<IMongoCollection<GlobalRatingSuggestionPreferenceDocument>> preferences =
            new Mock<IMongoCollection<GlobalRatingSuggestionPreferenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<GlobalRatingSuggestionInteractionDocument>> interactions =
            new Mock<IMongoCollection<GlobalRatingSuggestionInteractionDocument>>(MockBehavior.Strict);
        GlobalRatingSuggestionStateRepository repository =
            new GlobalRatingSuggestionStateRepository(
                states.Object,
                preferences.Object,
                interactions.Object);

        bool recorded = await repository.TryRecordInteractionAsync(
            "owner-1",
            RatingTargetType.Park,
            "park-1",
            RatingUpdatedAtUtc,
            GlobalRatingSuggestionInteractionType.Accepted,
            RatingUpdatedAtUtc.AddHours(1),
            CancellationToken.None);

        Assert.False(recorded);
        states.VerifyAll();
        interactions.VerifyNoOtherCalls();
        preferences.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TerminalTransition_UsesExpectedPresentationAndAwaitingStateAsAtomicFence()
    {
        FilterDefinition<GlobalRatingSuggestionStateDocument>? capturedFilter = null;
        Mock<IMongoCollection<GlobalRatingSuggestionStateDocument>> states =
            new Mock<IMongoCollection<GlobalRatingSuggestionStateDocument>>(MockBehavior.Strict);
        states.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<GlobalRatingSuggestionStateDocument>>(),
                It.IsAny<UpdateDefinition<GlobalRatingSuggestionStateDocument>>(),
                It.Is<UpdateOptions>(static options => !options.IsUpsert),
                CancellationToken.None))
            .Callback((
                FilterDefinition<GlobalRatingSuggestionStateDocument> filter,
                UpdateDefinition<GlobalRatingSuggestionStateDocument> _,
                UpdateOptions _,
                CancellationToken _) => capturedFilter = filter)
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        Mock<IMongoCollection<GlobalRatingSuggestionPreferenceDocument>> preferences =
            new Mock<IMongoCollection<GlobalRatingSuggestionPreferenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<GlobalRatingSuggestionInteractionDocument>> interactions =
            new Mock<IMongoCollection<GlobalRatingSuggestionInteractionDocument>>(MockBehavior.Strict);
        interactions.Setup(value => value.InsertOneAsync(
                It.Is<GlobalRatingSuggestionInteractionDocument>(static document =>
                    document.InteractionType == GlobalRatingSuggestionInteractionType.Dismissed),
                null,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        GlobalRatingSuggestionStateRepository repository =
            new GlobalRatingSuggestionStateRepository(
                states.Object,
                preferences.Object,
                interactions.Object);

        bool recorded = await repository.TryRecordInteractionAsync(
            "owner-1",
            RatingTargetType.ParkItem,
            "item-1",
            RatingUpdatedAtUtc,
            GlobalRatingSuggestionInteractionType.Dismissed,
            RatingUpdatedAtUtc.AddHours(1),
            CancellationToken.None);

        Assert.True(recorded);
        Assert.NotNull(capturedFilter);
        BsonDocument rendered = Render(capturedFilter);
        Assert.Equal(RatingUpdatedAtUtc, rendered["lastPresentedAtUtc"].ToUniversalTime());
        Assert.True(rendered["isAwaitingResolution"].AsBoolean);
        states.VerifyAll();
        interactions.VerifyAll();
        preferences.VerifyNoOtherCalls();
    }

    private static GlobalRatingSuggestionRatingSourceDocument Rating(
        RatingTargetType targetType,
        string targetId,
        string parkId,
        double value)
    {
        return new GlobalRatingSuggestionRatingSourceDocument
        {
            TargetType = targetType,
            TargetId = targetId,
            ParkId = parkId,
            ParkItemCategory = targetType == RatingTargetType.ParkItem
                ? ParkItemCategory.Attraction
                : null,
            ParkItemType = targetType == RatingTargetType.ParkItem
                ? ParkItemType.RollerCoaster
                : null,
            Value = value,
            UpdatedAtUtc = RatingUpdatedAtUtc,
        };
    }

    private static GlobalRatingSuggestionOccurrenceSourceDocument Ride(
        string visitId,
        string parkId,
        string itemId,
        long fence,
        byte valueHalfSteps)
    {
        return new GlobalRatingSuggestionOccurrenceSourceDocument
        {
            VisitId = visitId,
            ParkId = parkId,
            ParkItemId = itemId,
            ContentMutationFenceToken = fence,
            AssessmentValueHalfSteps = valueHalfSteps,
            AssessmentUpdatedAtUtc = RatingUpdatedAtUtc.AddDays(1),
        };
    }

    private static BsonDocument Render<TDocument>(IndexKeysDefinition<TDocument> keys)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        return keys.Render(new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render<TDocument>(FilterDefinition<TDocument> filter)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        return filter.Render(new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }
}
