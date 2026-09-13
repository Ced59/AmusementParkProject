using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class PassportProfileShareSelectedParkDocument
{
    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("countryCode")]
    [BsonIgnoreIfNull]
    public string? CountryCode { get; set; }
}
