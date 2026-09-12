using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class PassportProfileShareCountryDocument
{
    [BsonElement("countryCode")]
    public string CountryCode { get; set; } = string.Empty;

    [BsonElement("parkCount")]
    public long ParkCount { get; set; }

    [BsonElement("visitCount")]
    public long VisitCount { get; set; }
}
