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