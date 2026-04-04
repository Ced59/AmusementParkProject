using Common.General;
using MongoDB.Bson.Serialization.Attributes;

namespace WebAPI.Features.CaptainCoaster.Models
{
    public sealed class CaptainCoasterCoasterSnapshot : ModelBase
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
        // Parc d'appartenance
        // -----------------------------------------------------------------------

        [BsonElement("parkCaptainCoasterId")]
        [BsonIgnoreIfNull]
        public string? ParkCaptainCoasterId { get; set; }

        [BsonElement("parkName")]
        [BsonIgnoreIfNull]
        public string? ParkName { get; set; }

        // -----------------------------------------------------------------------
        // Caractéristiques techniques (staging uniquement)
        // -----------------------------------------------------------------------

        [BsonElement("manufacturer")]
        [BsonIgnoreIfNull]
        public string? Manufacturer { get; set; }

        [BsonElement("model")]
        [BsonIgnoreIfNull]
        public string? Model { get; set; }

        [BsonElement("materialType")]
        [BsonIgnoreIfNull]
        public string? MaterialType { get; set; }

        [BsonElement("seatingType")]
        [BsonIgnoreIfNull]
        public string? SeatingType { get; set; }

        /// <summary>
        /// Type de système de lancement (ex : "Propulsion électrique", "Lift à pneus", "LSM", …).
        /// Donnée technique du scraper, stockée en staging.
        /// </summary>
        [BsonElement("launchType")]
        [BsonIgnoreIfNull]
        public string? LaunchType { get; set; }

        [BsonElement("restraint")]
        [BsonIgnoreIfNull]
        public string? Restraint { get; set; }

        /// <summary>
        /// Indique si le coaster est à lancement propulsé (vs gravité / lift classique).
        /// </summary>
        [BsonElement("isLaunched")]
        public bool IsLaunched { get; set; }

        // -----------------------------------------------------------------------
        // Dimensions et performances
        // -----------------------------------------------------------------------

        [BsonElement("speedInKmH")]
        [BsonIgnoreIfNull]
        public double? SpeedInKmH { get; set; }

        [BsonElement("heightInMeters")]
        [BsonIgnoreIfNull]
        public double? HeightInMeters { get; set; }

        [BsonElement("lengthInMeters")]
        [BsonIgnoreIfNull]
        public double? LengthInMeters { get; set; }

        [BsonElement("dropInMeters")]
        [BsonIgnoreIfNull]
        public double? DropInMeters { get; set; }

        [BsonElement("inversionCount")]
        [BsonIgnoreIfNull]
        public int? InversionCount { get; set; }

        // -----------------------------------------------------------------------
        // Statut et dates
        // Converties depuis le texte brut du scraper via PartialDateParser.
        // -----------------------------------------------------------------------

        [BsonElement("status")]
        [BsonIgnoreIfNull]
        public string? Status { get; set; }

        /// <summary>
        /// Date d'ouverture parsée depuis le texte brut.
        /// Dates partielles (année seule, mois/année) converties au 1er jour de la période.
        /// </summary>
        [BsonElement("openingDate")]
        [BsonIgnoreIfNull]
        public DateTime? OpeningDate { get; set; }

        /// <summary>
        /// Date de fermeture parsée depuis le texte brut.
        /// </summary>
        [BsonElement("closingDate")]
        [BsonIgnoreIfNull]
        public DateTime? ClosingDate { get; set; }

        /// <summary>
        /// Horodatage du scraping de cette fiche.
        /// </summary>
        [BsonElement("scrapedAtUtc")]
        [BsonIgnoreIfNull]
        public DateTime? ScrapedAtUtc { get; set; }
    }
}
