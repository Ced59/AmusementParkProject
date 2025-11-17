using Common.General;
using Common.General.Localization;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Countries
{
    public class Country : ModelBase
    {
        // Code ISO alpha-2 : FR, BE, US, ...
        [BsonElement("isoCode")]
        public string IsoCode { get; set; } = default!;

        // Liste des noms localisés
        [BsonElement("names")]
        public List<LocalizedItem<string>> Names { get; set; } = new();
    }
}