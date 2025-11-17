using Common.General;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Parks
{
    public class Park : GeolocatedEntity
    {
        [BsonElement("name")]
        public string? Name { get; set; }

        [BsonElement("countryCode")]
        public string? CountryCode { get; set; }

        [BsonElement("isVisible")]
        [BsonRepresentation(BsonType.Boolean)]
        public bool IsVisible { get; set; } = false;
    }
}