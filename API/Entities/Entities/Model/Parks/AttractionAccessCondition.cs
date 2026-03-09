using Common.General.Localization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Parks
{
    [BsonIgnoreExtraElements]
    public class AttractionAccessCondition
    {
        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public AttractionAccessConditionType Type { get; set; }

        [BsonElement("isCustom")]
        [BsonIgnoreIfNull]
        public bool? IsCustom { get; set; }

        [BsonElement("value")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Double)]
        public double? Value { get; set; }

        [BsonElement("unit")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.String)]
        public AttractionAccessConditionUnit? Unit { get; set; }

        [BsonElement("requiresAccompaniment")]
        [BsonIgnoreIfNull]
        public bool? RequiresAccompaniment { get; set; }

        [BsonElement("minimumCompanionAge")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Int32)]
        public int? MinimumCompanionAge { get; set; }

        [BsonElement("label")]
        [BsonIgnoreIfNull]
        public List<LocalizedItem<string>>? Label { get; set; }

        [BsonElement("description")]
        [BsonIgnoreIfNull]
        public List<LocalizedItem<string>>? Description { get; set; }

        [BsonElement("displayOrder")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Int32)]
        public int? DisplayOrder { get; set; }
    }
}
