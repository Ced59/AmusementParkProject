using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;

/// <summary>
/// Point géographique embarqué.
/// </summary>
public sealed class GeoPointDocument
{
    [BsonElement("latitude")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.Double)]
    public double? Latitude { get; set; }

    [BsonElement("longitude")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.Double)]
    public double? Longitude { get; set; }
}
