using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripInvitationMongoDefinitionsTests
{
    [Fact]
    public void Indexes_ShouldProtectTokensOperationsCapacityAndExpiry()
    {
        IReadOnlyCollection<CreateIndexModel<TripInvitationDocument>> indexes =
            TripInvitationRepository.BuildIndexes();

        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_invitation_token_hash"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_invitation_operation"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_invitation_active_slot"
            && index.Options.Unique == true
            && index.Options.PartialFilterExpression is not null);
        Assert.Contains(indexes, index => index.Options.Name == "ttl_trip_invitation_retention"
            && index.Options.ExpireAfter == TimeSpan.Zero);
        Assert.Contains(indexes, index => index.Options.Name == "ttl_trip_invitation_prepared"
            && index.Options.ExpireAfter == TimeSpan.Zero);
    }

    [Fact]
    public void ExpirationCleanup_ShouldUseMongoServerTimeBeforeReleasingAnActiveSlot()
    {
        FilterDefinition<TripInvitationDocument> filter =
            TripInvitationRepository.BuildElapsedExpirationFilter();
        BsonDocument rendered = filter.Render(new RenderArgs<TripInvitationDocument>(
            BsonSerializer.LookupSerializer<TripInvitationDocument>(),
            BsonSerializer.SerializerRegistry));

        Assert.Equal("$$NOW", rendered["$expr"]["$gte"][0].AsString);
        Assert.Equal("$expiresAtUtc", rendered["$expr"]["$gte"][1].AsString);
    }

    [Fact]
    public void PublicLookup_ShouldUseMongoServerTimeInsteadOfTheWebNodeClock()
    {
        FilterDefinition<TripInvitationDocument> filter = TripInvitationRepository.BuildPublicActiveFilter();
        BsonDocument rendered = filter.Render(new RenderArgs<TripInvitationDocument>(
            BsonSerializer.LookupSerializer<TripInvitationDocument>(),
            BsonSerializer.SerializerRegistry));

        Assert.Equal("$$NOW", rendered["$expr"]["$lt"][0].AsString);
        Assert.Equal("$expiresAtUtc", rendered["$expr"]["$lt"][1].AsString);
    }
}
