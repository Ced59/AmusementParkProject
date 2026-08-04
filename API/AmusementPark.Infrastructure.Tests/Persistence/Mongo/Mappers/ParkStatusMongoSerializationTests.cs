using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class ParkStatusMongoSerializationTests
{
    [Theory]
    [InlineData(ParkStatus.Operating, "Operating")]
    [InlineData(ParkStatus.ClosedDefinitively, "ClosedDefinitively")]
    [InlineData(ParkStatus.Planned, "Planned")]
    [InlineData(ParkStatus.UnderConstruction, "UnderConstruction")]
    [InlineData(ParkStatus.TemporarilyClosed, "TemporarilyClosed")]
    [InlineData(ParkStatus.Cancelled, "Cancelled")]
    public void ParkDocument_ShouldSerializeAndReadStatusAsCanonicalString(ParkStatus status, string storedValue)
    {
        ParkDocument document = new ParkDocument { Status = status };

        BsonDocument bson = document.ToBsonDocument();
        ParkDocument roundTrip = BsonSerializer.Deserialize<ParkDocument>(bson);

        Assert.Equal(storedValue, bson["status"].AsString);
        Assert.Equal(status, roundTrip.Status);
    }

    [Theory]
    [InlineData(ParkStatus.Planned, "Planned")]
    [InlineData(ParkStatus.UnderConstruction, "UnderConstruction")]
    [InlineData(ParkStatus.Operating, "Operating")]
    [InlineData(ParkStatus.TemporarilyClosed, "TemporarilyClosed")]
    [InlineData(ParkStatus.ClosedDefinitively, "ClosedDefinitively")]
    [InlineData(ParkStatus.Cancelled, "Cancelled")]
    public void SearchItemDocument_ShouldSerializeParkStatusAsCanonicalString(ParkStatus status, string storedValue)
    {
        SearchItemDocument document = new SearchItemDocument { ParkStatus = status };

        BsonDocument bson = document.ToBsonDocument();
        SearchItemDocument roundTrip = BsonSerializer.Deserialize<SearchItemDocument>(bson);

        Assert.Equal(storedValue, bson["parkStatus"].AsString);
        Assert.Equal(status, roundTrip.ParkStatus);
    }
}
