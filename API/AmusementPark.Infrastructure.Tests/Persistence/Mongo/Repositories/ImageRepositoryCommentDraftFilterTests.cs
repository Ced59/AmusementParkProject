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
                "attempt-loser",
                7);

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
        Assert.True(ContainsFieldValue(
            rendered,
            "pendingCommentRevision",
            new BsonInt64(7)));
        Assert.True(ContainsFieldComparison(
            rendered,
            "abortedReservationTokens",
            "$ne",
            new BsonString("attempt-loser")));
    }

    [Fact]
    public void BuildCompleteCommentDraftUploadFilter_ShouldRequireExactGuardAndNoCleanupClaim()
    {
        DateTime guardUtc =
            new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildCompleteCommentDraftUploadFilter(
                "image-1",
                "author-1",
                "upload-token",
                guardUtc);

        BsonDocument rendered = Render(filter);

        Assert.True(ContainsFieldValue(
            rendered,
            "commentDraftUploadToken",
            new BsonString("upload-token")));
        Assert.True(ContainsFieldValue(
            rendered,
            "cleanupRequestedAt",
            new BsonDateTime(guardUtc)));
        Assert.True(ContainsFieldValue(
            rendered,
            "cleanupClaimToken",
            BsonNull.Value));
        Assert.True(ContainsFieldValue(
            rendered,
            "pendingCommentId",
            BsonNull.Value));
    }

    [Fact]
    public void BuildCompleteCommentDraftUploadUpdate_ShouldClearOnlyTheProvisionalGuard()
    {
        BsonDocument rendered = Render(
            ImageRepository.BuildCompleteCommentDraftUploadUpdate());
        BsonDocument unset = rendered["$unset"].AsBsonDocument;

        Assert.True(unset.Contains("commentDraftUploadToken"));
        Assert.True(unset.Contains("cleanupRequestedAt"));
        Assert.True(unset.Contains("cleanupCommentRevision"));
        Assert.False(unset.Contains("cleanupClaimToken"));
    }

    [Fact]
    public void BuildAbortCommentDraftReservationUpdate_ShouldPersistTheAttemptToken()
    {
        BsonDocument rendered = Render(
            ImageRepository.BuildAbortCommentDraftReservationUpdate(
                "attempt-token"));

        Assert.Equal(
            "attempt-token",
            rendered["$addToSet"].AsBsonDocument[
                "abortedReservationTokens"].AsString);
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
                null,
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

    [Fact]
    public void BuildRequestCommentImagesCleanupFilter_ShouldMatchPublishedOrReservedDraft()
    {
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildRequestCommentImagesCleanupFilter(
                new[] { "image-1" },
                "comment-1");

        BsonDocument rendered = Render(filter);

        Assert.True(ContainsField(rendered, "_id"));
        Assert.True(ContainsFieldValue(
            rendered,
            "ownerId",
            new BsonString("comment-1")));
        Assert.True(ContainsFieldValue(
            rendered,
            "pendingCommentId",
            new BsonString("comment-1")));
        Assert.True(ContainsFieldValue(
            rendered,
            "ownerType",
            new BsonString("Comment")));
        Assert.True(ContainsFieldValue(
            rendered,
            "ownerType",
            new BsonString("CommentDraft")));
        Assert.False(ContainsField(rendered, "cleanupClaimToken"));
    }

    [Fact]
    public void BuildRequestCommentImagesCleanupUpdate_ShouldAdvanceCleanupAndRevisionMonotonically()
    {
        DateTime cleanupUtc =
            new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        UpdateDefinition<ImageDocument> update =
            ImageRepository.BuildRequestCommentImagesCleanupUpdate(
                8,
                cleanupUtc);

        BsonDocument rendered = Render(update);
        BsonDocument max = rendered["$max"].AsBsonDocument;

        Assert.Equal(8, max["cleanupCommentRevision"].AsInt64);
        Assert.Equal(
            new BsonDateTime(cleanupUtc),
            max["cleanupRequestedAt"]);
        Assert.Equal(
            new BsonDateTime(cleanupUtc),
            max["reservationReconcileAfter"]);
        Assert.False(
            rendered["$set"].AsBsonDocument.Contains(
                "cleanupRequestedAt"));
    }

    [Fact]
    public void BuildFinalizeCommentDraftUpdate_ShouldPreserveNewerCleanupRequest()
    {
        UpdateDefinition<ImageDocument> update =
            ImageRepository.BuildFinalizeCommentDraftUpdate(
                "author-1",
                "comment-1");

        BsonDocument unset = Render(update)["$unset"].AsBsonDocument;

        Assert.True(unset.Contains("pendingReservationToken"));
        Assert.True(unset.Contains("pendingCommentRevision"));
        Assert.True(unset.Contains("pendingReservationExpiresAt"));
        Assert.True(unset.Contains("abortedReservationTokens"));
        Assert.True(unset.Contains("reservationReconcileAfter"));
        Assert.False(unset.Contains("cleanupRequestedAt"));
        Assert.False(unset.Contains("cleanupCommentRevision"));
    }

    [Fact]
    public void BuildUnchangedClaimedCommentImageFilter_ShouldRequireTimestampAndToken()
    {
        DateTime observedCleanupUtc =
            new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildUnchangedClaimedCommentImageFilter(
                "image-1",
                AmusementPark.Core.Domain.Images.ImageOwnerType.Comment,
                "comment-1",
                observedCleanupUtc,
                6,
                "claim-owner");

        BsonDocument rendered = Render(filter);

        Assert.True(ContainsFieldValue(
            rendered,
            "cleanupClaimToken",
            new BsonString("claim-owner")));
        Assert.True(ContainsFieldValue(
            rendered,
            "cleanupRequestedAt",
            new BsonDateTime(observedCleanupUtc)));
        Assert.True(ContainsFieldValue(
            rendered,
            "cleanupCommentRevision",
            new BsonInt64(6)));
    }

    [Fact]
    public void BuildReleaseCommentImageCleanupClaimUpdate_ShouldPreserveNewerRequest()
    {
        UpdateDefinition<ImageDocument> update =
            ImageRepository.BuildReleaseCommentImageCleanupClaimUpdate();

        BsonDocument rendered = Render(update);
        BsonDocument unset = rendered["$unset"].AsBsonDocument;

        Assert.True(unset.Contains("cleanupClaimToken"));
        Assert.True(unset.Contains("cleanupClaimedUntil"));
        Assert.False(unset.Contains("cleanupRequestedAt"));
    }

    [Fact]
    public void BuildCancelClaimedCommentImageCleanupUpdate_ShouldClearUnchangedRequest()
    {
        UpdateDefinition<ImageDocument> update =
            ImageRepository.BuildCancelClaimedCommentImageCleanupUpdate();

        BsonDocument rendered = Render(update);
        BsonDocument unset = rendered["$unset"].AsBsonDocument;

        Assert.True(unset.Contains("cleanupClaimToken"));
        Assert.True(unset.Contains("cleanupClaimedUntil"));
        Assert.True(unset.Contains("cleanupRequestedAt"));
        Assert.True(unset.Contains("cleanupCommentRevision"));
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

    private static BsonDocument Render(UpdateDefinition<ImageDocument> update)
    {
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments =
            new RenderArgs<ImageDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return update.Render(arguments).AsBsonDocument;
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

    private static bool ContainsField(
        BsonValue value,
        string fieldName)
    {
        if (value is BsonDocument document)
        {
            return document.Contains(fieldName)
                || document.Elements.Any(
                    element => ContainsField(element.Value, fieldName));
        }

        return value is BsonArray array
            && array.Any(item => ContainsField(item, fieldName));
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
