using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceCreationPreparationItemDocument
{
    [BsonElement("index")]
    public int Index { get; set; }

    [BsonElement("parkItemId")]
    [BsonIgnoreIfNull]
    public string? ParkItemId { get; set; }

    [BsonElement("historicalConsistency")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalConsistency HistoricalConsistency { get; set; }

    [BsonElement("historicalTargetName")]
    [BsonIgnoreIfNull]
    public string? HistoricalTargetName { get; set; }

    [BsonElement("historicalTargetCategory")]
    [BsonIgnoreIfNull]
    public string? HistoricalTargetCategory { get; set; }
}
