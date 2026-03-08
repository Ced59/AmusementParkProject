using Common.General;
using Common.General.Localization;
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

        [BsonElement("type")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.String)]
        public ParkType? Type { get; set; }

        [BsonElement("founderId")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.String)]
        public string? FounderId { get; set; }

        [BsonElement("operatorId")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.String)]
        public string? OperatorId { get; set; }

        [BsonElement("descriptions")]
        public List<LocalizedItem<string>> Descriptions { get; set; } = new();

        [BsonElement("isVisible")]
        [BsonRepresentation(BsonType.Boolean)]
        public bool IsVisible { get; set; } = false;

        [BsonElement("websiteUrl")]
        public string? WebSiteUrl { get; set; }

        [BsonElement("street")]
        public string? Street { get; set; }

        [BsonElement("city")]
        public string? City { get; set; }

        [BsonElement("postalCode")]
        public string? PostalCode { get; set; }

        /// <summary>
        /// Id de l'image actuellement utilisée comme logo.
        /// Permet un accès ultra rapide côté front sans requête supplémentaire.
        /// </summary>
        [BsonElement("currentLogoImageId")]
        public string? CurrentLogoImageId { get; set; }
    }
}
