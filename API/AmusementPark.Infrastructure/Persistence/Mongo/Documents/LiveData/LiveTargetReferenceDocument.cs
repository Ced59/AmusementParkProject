using AmusementPark.Core.Domain.LiveData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class LiveTargetReferenceDocument
{
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public LiveTargetType Type { get; set; }

    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("parkDisplayName")]
    public string ParkDisplayName { get; set; } = string.Empty;

    [BsonElement("countryCode")]
    public string CountryCode { get; set; } = string.Empty;
}
