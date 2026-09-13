using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class MongoShareAccountDeletionStoreTests
{
    [Fact]
    public void BuildParticipantFilter_ShouldMatchEitherComparisonRole()
    {
        string json = Render(
            MongoShareAccountDeletionStore.BuildParticipantFilter("user-1"))
            .ToJson();

        Assert.Contains("creatorUserId", json, StringComparison.Ordinal);
        Assert.Contains("acceptorUserId", json, StringComparison.Ordinal);
        Assert.Contains("user-1", json, StringComparison.Ordinal);
        Assert.Contains("$or", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildModerationReportFilter_ShouldKeepPublicationAndComparisonTypesSeparated()
    {
        FilterDefinition<BsonDocument>? filter =
            MongoShareAccountDeletionStore.BuildModerationReportFilter(
                new[] { "publication-1" },
                new[] { "comparison-1" });

        string json = Render(Assert.IsAssignableFrom<FilterDefinition<BsonDocument>>(filter))
            .ToJson();
        Assert.Contains("publication-1", json, StringComparison.Ordinal);
        Assert.Contains("comparison-1", json, StringComparison.Ordinal);
        Assert.Contains("ProfileComparison", json, StringComparison.Ordinal);
        Assert.Contains("PassportProfile", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSourceScopeKeys_ShouldIncludeEveryOwnerScopedRevision()
    {
        BsonDocument publication = new BsonDocument
        {
            { "_id", "publication-1" },
            { "sourceScopeKey", "visit-recap:user-1:visit-1" },
        };

        string[] scopes = MongoShareAccountDeletionStore.BuildSourceScopeKeys(
            "user-1",
            new[] { publication },
            new[] { "passport-profile:user-1:preview-only" });

        Assert.Contains("visit-recap:user-1:visit-1", scopes);
        Assert.Contains("passport-profile:user-1:preview-only", scopes);
        Assert.Contains(PersonalRankingShareSourceScope.Create("user-1"), scopes);
        Assert.Contains(PublicIdentityShareSourceScope.CreateDisplayName("user-1"), scopes);
        Assert.Contains(PublicIdentityShareSourceScope.CreateAvatar("user-1"), scopes);
        Assert.Contains(PassportProfileShareSourceScope.Create("user-1"), scopes);
        Assert.Contains(PassportProfileShareSourceScope.CreateCoordination("user-1"), scopes);
        Assert.Equal(scopes.Length, scopes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void BuildSourceRevisionFilter_ShouldCoverPreviewOnlyOwnerScopes()
    {
        string json = Render(
            MongoShareAccountDeletionStore.BuildSourceRevisionFilter(
                "user-1",
                new[] { "registered-scope" }))
            .ToJson();

        Assert.Contains("registered-scope", json, StringComparison.Ordinal);
        Assert.Contains(VisitRecapShareSourceScope.CreateOwnerPrefix("user-1"), json);
        Assert.Contains(YearRecapShareSourceScope.CreateOwnerPrefix("user-1"), json);
        Assert.Contains(
            PassportProfileShareSourceScope.CreateFingerprintOwnerPrefix("user-1"),
            json);
        Assert.Contains(PersonalRankingShareSourceScope.CreateRatingOwnerPrefix("user-1"), json);
        Assert.Contains("$gte", json, StringComparison.Ordinal);
        Assert.Contains("$lt", json, StringComparison.Ordinal);
    }

    private static BsonDocument Render(FilterDefinition<BsonDocument> filter)
    {
        IBsonSerializer<BsonDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
        return filter.Render(new RenderArgs<BsonDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }
}
