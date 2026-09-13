using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceReorderAllocationDocument
{
    [BsonElement("index")]
    public int Index { get; set; }

    [BsonElement("occurrenceId")]
    public string OccurrenceId { get; set; } = string.Empty;

    [BsonElement("expectedVersion")]
    public long ExpectedVersion { get; set; }

    [BsonElement("previousSortPosition")]
    public long PreviousSortPosition { get; set; }

    [BsonElement("resultSortPosition")]
    public long ResultSortPosition { get; set; }

    [BsonElement("resultVersion")]
    public long ResultVersion { get; set; }

    [BsonElement("resultUpdatedAtUtc")]
    public DateTime ResultUpdatedAtUtc { get; set; }
}
