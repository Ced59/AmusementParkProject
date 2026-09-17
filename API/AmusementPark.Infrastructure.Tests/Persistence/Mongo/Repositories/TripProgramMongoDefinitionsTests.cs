using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripProgramMongoDefinitionsTests
{
    [Fact]
    public void CandidateIndexes_ShouldGuaranteeOneParkPerTripAndExpireReservedShells()
    {
        IReadOnlyCollection<CreateIndexModel<TripParkCandidateDocument>> indexes =
            TripParkCandidateRepository.BuildIndexes();

        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_candidate_plan_park"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "ix_trip_candidate_plan_order");
        Assert.Contains(indexes, index => index.Options.Name == "ttl_trip_candidate_reserved"
            && index.Options.ExpireAfter == TimeSpan.Zero);
    }

    [Fact]
    public void DayIndexes_ShouldGuaranteeOneProgramPerLocalDate()
    {
        IReadOnlyCollection<CreateIndexModel<TripDayPlanDocument>> indexes =
            TripDayPlanRepository.BuildIndexes();

        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_day_plan_date"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "ttl_trip_day_plan_reserved"
            && index.Options.ExpireAfter == TimeSpan.Zero);
    }

    [Fact]
    public void CreationGuard_ShouldUseMongoServerTime()
    {
        FilterDefinition<TripParkCandidateDocument> filter =
            TripChildMutationMongoDefinitions.BuildCreationLeaseGuard<TripParkCandidateDocument>();
        BsonDocument rendered = filter.Render(new RenderArgs<TripParkCandidateDocument>(
            BsonSerializer.LookupSerializer<TripParkCandidateDocument>(),
            BsonSerializer.SerializerRegistry));

        Assert.Equal("$$NOW", rendered["$expr"]["$lt"][0].AsString);
        Assert.Equal("$leaseExpiresAtUtc", rendered["$expr"]["$lt"][1].AsString);
    }

    [Fact]
    public void ExpiredCreationGuard_ShouldOnlyAllowReclaimAfterServerExpiry()
    {
        FilterDefinition<TripDayPlanDocument> filter =
            TripChildMutationMongoDefinitions.BuildExpiredCreationLeaseGuard<TripDayPlanDocument>();
        BsonDocument rendered = filter.Render(new RenderArgs<TripDayPlanDocument>(
            BsonSerializer.LookupSerializer<TripDayPlanDocument>(),
            BsonSerializer.SerializerRegistry));

        Assert.Equal("$leaseExpiresAtUtc", rendered["$expr"]["$lte"][0].AsString);
        Assert.Equal("$$NOW", rendered["$expr"]["$lte"][1].AsString);
    }

    [Fact]
    public void RootBarrier_ShouldIgnoreExpiredLeasesAndUseMongoServerTime()
    {
        FilterDefinition<TripPlanDocument> filter =
            TripPlanMongoDefinitions.BuildNoActiveChildLeaseFilter();
        BsonDocument rendered = filter.Render(new RenderArgs<TripPlanDocument>(
            BsonSerializer.LookupSerializer<TripPlanDocument>(),
            BsonSerializer.SerializerRegistry));

        string json = rendered.ToJson();
        Assert.Contains("$$NOW", json, StringComparison.Ordinal);
        Assert.Contains("activeChildMutationLeases", json, StringComparison.Ordinal);
    }
}
