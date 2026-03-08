using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Parks
{
    [BsonIgnoreExtraElements]
    public class AttractionLocations
    {
        [BsonElement("entrance")]
        [BsonIgnoreIfNull]
        public AttractionLocationPoint? Entrance { get; set; }

        [BsonElement("exit")]
        [BsonIgnoreIfNull]
        public AttractionLocationPoint? Exit { get; set; }

        [BsonElement("fastPassEntrance")]
        [BsonIgnoreIfNull]
        public AttractionLocationPoint? FastPassEntrance { get; set; }

        [BsonElement("reducedMobilityEntrance")]
        [BsonIgnoreIfNull]
        public AttractionLocationPoint? ReducedMobilityEntrance { get; set; }
    }
}
