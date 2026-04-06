using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;

/// <summary>
/// Document Mongo d'un pays.
/// </summary>
public sealed class CountryDocument : MongoDocumentBase
{
    [BsonElement("isoCode")]
    public string IsoCode { get; set; } = string.Empty;

    [BsonElement("names")]
    public List<LocalizedTextDocument> Names { get; set; } = new();
}
