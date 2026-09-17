using AmusementPark.Core.Domain.Trips;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripDayBlockDocument
{
    [BsonElement("blockId")]
    public string BlockId { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public TripDayBlockType Type { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("details")]
    [BsonIgnoreIfNull]
    public string? Details { get; set; }

    [BsonElement("localTime")]
    [BsonIgnoreIfNull]
    public string? LocalTime { get; set; }

    [BsonElement("sortPosition")]
    public long SortPosition { get; set; }
}
