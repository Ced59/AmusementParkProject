using System.Collections.Generic;
using Common.General;
using Common.General.Localization;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Parks
{
    public class AttractionManufacturer : ModelBase
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("biography")]
        public List<LocalizedItem<string>> Biography { get; set; } = new();
    }
}
