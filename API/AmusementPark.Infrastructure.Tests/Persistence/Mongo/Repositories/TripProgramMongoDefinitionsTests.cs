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
        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_candidate_plan_operation"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "ix_trip_candidate_plan_order");
        Assert.Contains(indexes, index => index.Options.Name == "ttl_trip_candidate_reserved"
            && index.Options.ExpireAfter == TimeSpan.Zero);
        Assert.Contains(indexes, index => index.Options.Name == "ttl_trip_candidate_creation_tombstone"
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
    public void PreferenceIndexes_ShouldGuaranteeOneChoicePerUserAndItem()
    {
        IReadOnlyCollection<CreateIndexModel<TripItemPreferenceDocument>> indexes =
            TripPreferenceRepository.BuildIndexes();

        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_preference_plan_user_item"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "ix_trip_preference_plan_item");
        Assert.Contains(indexes, index => index.Options.Name == "ttl_trip_preference_reserved"
            && index.Options.ExpireAfter == TimeSpan.Zero);
    }

    [Fact]
    public void DecisionIndexes_ShouldGuaranteeOneDecisionPerTripAndItem()
    {
        IReadOnlyCollection<CreateIndexModel<TripItemDecisionDocument>> indexes =
            TripItemDecisionRepository.BuildIndexes();

        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_item_decision_plan_item"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "ttl_trip_item_decision_reserved"
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

    [Fact]
    public void OrderDocuments_ShouldApplyTheAtomicOrderAndAppendUnreferencedCandidates()
    {
        DateTime nowUtc = new(2027, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        TripParkCandidateDocument first = new()
        {
            Id = "candidate-1",
            SortPosition = 1024,
            CreatedAt = nowUtc,
        };
        TripParkCandidateDocument second = new()
        {
            Id = "candidate-2",
            SortPosition = 2048,
            CreatedAt = nowUtc.AddMinutes(1),
        };
        TripParkCandidateDocument third = new()
        {
            Id = "candidate-3",
            SortPosition = 3072,
            CreatedAt = nowUtc.AddMinutes(2),
        };

        IReadOnlyList<TripParkCandidateDocument> ordered =
            TripParkCandidateRepository.OrderDocuments(
                new[] { first, second, third },
                new[] { third.Id, "deleted-candidate", first.Id });

        Assert.Equal(
            new[] { third.Id, first.Id, second.Id },
            ordered.Select(static document => document.Id));
    }
}
