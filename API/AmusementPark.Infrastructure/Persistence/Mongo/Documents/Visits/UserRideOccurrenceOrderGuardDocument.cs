using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceOrderGuardDocument
{
    [BsonElement("occurrenceId")]
    public string OccurrenceId { get; set; } = string.Empty;

    [BsonElement("sortPosition")]
    public long SortPosition { get; set; }
}
