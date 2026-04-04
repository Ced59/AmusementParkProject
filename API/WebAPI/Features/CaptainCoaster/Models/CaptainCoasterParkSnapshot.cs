using Common.General;
using MongoDB.Bson.Serialization.Attributes;

namespace WebAPI.Features.CaptainCoaster.Models
{
    public sealed class CaptainCoasterParkSnapshot : GeolocatedEntity
    {
        [BsonElement("syncSessionId")]
        public string SyncSessionId { get; set; } = string.Empty;

        // -----------------------------------------------------------------------
        // Identité
        // -----------------------------------------------------------------------

        [BsonElement("captainCoasterId")]
        public string CaptainCoasterId { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("slug")]
        [BsonIgnoreIfNull]
        public string? Slug { get; set; }

        [BsonElement("sourceUrl")]
        [BsonIgnoreIfNull]
        public string? SourceUrl { get; set; }

        // -----------------------------------------------------------------------
        // Localisation (héritée : Latitude / Longitude depuis GeolocatedEntity)
        // Valeur par défaut (0, 0) quand le scraper ne fournit pas de coordonnées.
        // -----------------------------------------------------------------------

        [BsonElement("countryCode")]
        [BsonIgnoreIfNull]
        public string? CountryCode { get; set; }

        [BsonElement("countryRaw")]
        [BsonIgnoreIfNull]
        public string? CountryRaw { get; set; }

        // -----------------------------------------------------------------------
        // Statistiques issues du scraper (staging uniquement)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Nombre de coasters recensés dans ce parc par le scraper.
        /// </summary>
        [BsonElement("coasterCount")]
        public int CoasterCount { get; set; }

        /// <summary>
        /// Échantillon de noms de coasters fourni par le scraper pour faciliter le rapprochement.
        /// </summary>
        [BsonElement("sampleCoasterNames")]
        public List<string> SampleCoasterNames { get; set; } = new List<string>();

        /// <summary>
        /// Horodatage du scraping de cette fiche.
        /// </summary>
        [BsonElement("scrapedAtUtc")]
        [BsonIgnoreIfNull]
        public DateTime? ScrapedAtUtc { get; set; }
    }
}
