using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;

/// <summary>
/// Base commune des documents Mongo persistés.
/// </summary>
public abstract class MongoDocumentBase
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Base commune des documents géolocalisés.
/// </summary>
public abstract class MongoGeolocatedDocumentBase : MongoDocumentBase
{
    [BsonElement("latitude")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.Double)]
    public double? Latitude { get; set; }

    [BsonElement("longitude")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.Double)]
    public double? Longitude { get; set; }

    [BsonElement("location")]
    [BsonIgnoreIfNull]
    public GeoJsonPoint<GeoJson2DGeographicCoordinates>? Location { get; set; }

    /// <summary>
    /// Synchronise le champ GeoJSON à partir des coordonnées décimales.
    /// </summary>
    public void RefreshLocation()
    {
        if (Latitude.HasValue && Longitude.HasValue)
        {
            Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                new GeoJson2DGeographicCoordinates(Longitude.Value, Latitude.Value));
        }
        else
        {
            Location = null;
        }
    }
}

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
