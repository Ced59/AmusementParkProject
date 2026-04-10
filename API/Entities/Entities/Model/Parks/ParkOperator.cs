using Common.General;
using Common.General.Localization;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Parks;

public class ParkOperator : ModelBase
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public List<LocalizedItem<string>> Description { get; set; } = new();
}