using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Images
{
    public sealed class ImageGeoLocation
    {
        [BsonElement("latitude")]
        public double Latitude { get; set; }

        [BsonElement("longitude")]
        public double Longitude { get; set; }
    }
}
