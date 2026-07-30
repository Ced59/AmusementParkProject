using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ImageRepositoryCommentDraftFilterTests
{
    [Fact]
    public void BuildActiveCommentDraftsByOwnerFilter_ShouldExcludeDeletionRequestsButKeepReservations()
    {
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildActiveCommentDraftsByOwnerFilter("author-1");
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments =
            new RenderArgs<ImageDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);

        BsonDocument rendered = filter.Render(arguments);

        Assert.Equal("Comment", rendered["category"].AsString);
        Assert.Equal("CommentDraft", rendered["ownerType"].AsString);
        Assert.Equal("author-1", rendered["ownerId"].AsString);
        Assert.False(rendered["isPublished"].AsBoolean);
        BsonArray activeConditions = rendered["$or"].AsBsonArray;
        Assert.Contains(
            activeConditions,
            static condition => condition.AsBsonDocument.Contains("cleanupRequestedAt")
                && condition.AsBsonDocument["cleanupRequestedAt"].IsBsonNull);
        Assert.Contains(
            activeConditions,
            static condition => condition.AsBsonDocument.Contains("pendingCommentId")
                && condition.AsBsonDocument["pendingCommentId"]
                    .AsBsonDocument["$ne"].IsBsonNull);
    }

    [Fact]
    public void BuildCommentDraftReservationFilter_WhenCommentMatches_ShouldAlsoRequireAttemptToken()
    {
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildCommentDraftReservationFilter(
                "image-1",
                "author-1",
                "comment-1",
                "attempt-loser");

        BsonDocument rendered = Render(filter);

        Assert.True(ContainsFieldValue(
            rendered,
            "pendingReservationToken",
            new BsonString("attempt-loser")));
        Assert.True(ContainsFieldValue(
            rendered,
            "cleanupClaimToken",
            BsonNull.Value));
        Assert.True(ContainsFieldValue(
            rendered,
            "cleanupRequestedAt",
            BsonNull.Value));
    }

    [Fact]
    public void BuildPendingCommentDraftFilter_ShouldRequireCommentAndWinningAttemptToken()
    {
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildPendingCommentDraftFilter(
                "image-1",
                "author-1",
                "comment-1",
                "attempt-winner");

        BsonDocument rendered = Render(filter);

        Assert.Equal("comment-1", rendered["pendingCommentId"].AsString);
        Assert.Equal(
            "attempt-winner",
            rendered["pendingReservationToken"].AsString);
    }

    [Fact]
    public void BuildPublishedCommentImageReuseFilter_ShouldRejectAnyCleanupClaim()
    {
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildPublishedCommentImageReuseFilter(
                "image-1",
                "comment-1");

        BsonDocument rendered = Render(filter);

        Assert.True(rendered["cleanupClaimToken"].IsBsonNull);
    }

    [Fact]
    public void BuildCommentImageCleanupClaimFilter_ShouldRequireUnreservedEligibleDraft()
    {
        DateTime nowUtc =
            new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildCommentImageCleanupClaimFilter(
                "image-1",
                AmusementPark.Core.Domain.Images.ImageOwnerType.CommentDraft,
                "author-1",
                nowUtc,
                nowUtc.AddHours(-24),
                nowUtc);

        BsonDocument rendered = Render(filter);

        Assert.True(ContainsFieldValue(
            rendered,
            "pendingCommentId",
            BsonNull.Value));
        Assert.True(ContainsFieldValue(
            rendered,
            "pendingReservationToken",
            BsonNull.Value));
        Assert.True(ContainsFieldValue(
            rendered,
            "cleanupClaimToken",
            BsonNull.Value));
        Assert.True(ContainsFieldValue(
            rendered,
            "variantGenerationClaimToken",
            BsonNull.Value));
        Assert.True(ContainsFieldComparison(
            rendered,
            "variantGenerationClaimedUntil",
            "$lte",
            new BsonDateTime(nowUtc)));
    }

    [Fact]
    public void BuildVariantGenerationAcquireFilter_ShouldBeMutuallyExclusiveWithCleanupClaim()
    {
        DateTime nowUtc =
            new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        FilterDefinition<ImageDocument> filter =
            MongoImageVariantGenerationLease.BuildAcquireFilter(
                "comment/image-1",
                nowUtc);

        BsonDocument rendered = Render(filter);

        Assert.True(ContainsFieldValue(
            rendered,
            "cleanupClaimToken",
            BsonNull.Value));
        Assert.True(ContainsFieldComparison(
            rendered,
            "cleanupClaimedUntil",
            "$lte",
            new BsonDateTime(nowUtc)));
        Assert.True(ContainsFieldValue(
            rendered,
            "variantGenerationClaimToken",
            BsonNull.Value));
        Assert.True(ContainsFieldComparison(
            rendered,
            "variantGenerationClaimedUntil",
            "$lte",
            new BsonDateTime(nowUtc)));
    }

    [Fact]
    public void BuildVariantGenerationReleaseFilter_ShouldRequireOwningToken()
    {
        FilterDefinition<ImageDocument> filter =
            MongoImageVariantGenerationLease.BuildReleaseFilter(
                "comment/image-1",
                "lease-owner");

        BsonDocument rendered = Render(filter);

        Assert.Equal("comment/image-1", rendered["path"].AsString);
        Assert.Equal(
            "lease-owner",
            rendered["variantGenerationClaimToken"].AsString);
    }

    private static BsonDocument Render(FilterDefinition<ImageDocument> filter)
    {
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments =
            new RenderArgs<ImageDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }

    private static bool ContainsFieldValue(
        BsonValue value,
        string fieldName,
        BsonValue expected)
    {
        if (value is BsonDocument document)
        {
            if (document.TryGetValue(fieldName, out BsonValue? actual)
                && actual == expected)
            {
                return true;
            }

            return document.Elements.Any(
                element => ContainsFieldValue(
                    element.Value,
                    fieldName,
                    expected));
        }

        if (value is BsonArray array)
        {
            return array.Any(item => ContainsFieldValue(item, fieldName, expected));
        }

        return false;
    }

    private static bool ContainsFieldComparison(
        BsonValue value,
        string fieldName,
        string comparisonOperator,
        BsonValue expected)
    {
        if (value is BsonDocument document)
        {
            if (document.TryGetValue(fieldName, out BsonValue? fieldValue)
                && fieldValue is BsonDocument comparison
                && comparison.TryGetValue(
                    comparisonOperator,
                    out BsonValue? actual)
                && actual == expected)
            {
                return true;
            }

            return document.Elements.Any(
                element => ContainsFieldComparison(
                    element.Value,
                    fieldName,
                    comparisonOperator,
                    expected));
        }

        if (value is BsonArray array)
        {
            return array.Any(
                item => ContainsFieldComparison(
                    item,
                    fieldName,
                    comparisonOperator,
                    expected));
        }

        return false;
    }
}
