using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Parks
{
    [BsonIgnoreExtraElements]
    public class AttractionLocationPoint
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
}
