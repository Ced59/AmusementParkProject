using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;

/// <summary>
/// Valeur localisée persistée en Mongo.
/// </summary>
public sealed class LocalizedTextDocument
{
    [BsonElement("languageCode")]
    public string LanguageCode { get; set; } = string.Empty;

    [BsonElement("value")]
    public string? Value { get; set; }
}
