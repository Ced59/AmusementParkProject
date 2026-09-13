using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;

/// <summary>
/// Document embarqué des points fonctionnels d'une attraction.
/// </summary>
public sealed class AttractionLocationsDocument
{
    [BsonElement("entrance")]
    [BsonIgnoreIfNull]
    public GeoPointDocument? Entrance { get; set; }

    [BsonElement("exit")]
    [BsonIgnoreIfNull]
    public GeoPointDocument? Exit { get; set; }

    [BsonElement("fastPassEntrance")]
    [BsonIgnoreIfNull]
    public GeoPointDocument? FastPassEntrance { get; set; }

    [BsonElement("reducedMobilityEntrance")]
    [BsonIgnoreIfNull]
    public GeoPointDocument? ReducedMobilityEntrance { get; set; }
}
