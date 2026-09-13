using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class RideOccurrenceMomentDocument
{
    [BsonElement("localTime")]
    [BsonIgnoreIfNull]
    public string? LocalTime { get; set; }

    [BsonElement("isApproximate")]
    public bool IsApproximate { get; set; }
}
