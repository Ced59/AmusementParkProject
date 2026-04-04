using System;
using System.Collections.Generic;
using Common.General;
using MongoDB.Bson.Serialization.Attributes;

namespace WebAPI.Features.CaptainCoaster.Models
{
    public sealed class CaptainCoasterCoasterSnapshot : ModelBase
    {
        [BsonElement("syncSessionId")]
        public string SyncSessionId { get; set; } = string.Empty;

        [BsonElement("externalSource")]
        public string ExternalSource { get; set; } = "CaptainCoaster";

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

        [BsonElement("parkCaptainCoasterId")]
        [BsonIgnoreIfNull]
        public string? ParkCaptainCoasterId { get; set; }

        [BsonElement("parkName")]
        [BsonIgnoreIfNull]
        public string? ParkName { get; set; }

        [BsonElement("parkSlug")]
        [BsonIgnoreIfNull]
        public string? ParkSlug { get; set; }

        [BsonElement("country")]
        [BsonIgnoreIfNull]
        public string? Country { get; set; }

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

        [BsonElement("launchType")]
        [BsonIgnoreIfNull]
        public string? LaunchType { get; set; }

        [BsonElement("restraintType")]
        [BsonIgnoreIfNull]
        public string? RestraintType { get; set; }

        [BsonElement("isLaunched")]
        [BsonIgnoreIfNull]
        public bool? IsLaunched { get; set; }

        [BsonElement("heightInFeet")]
        [BsonIgnoreIfNull]
        public double? HeightInFeet { get; set; }

        [BsonElement("heightInMeters")]
        [BsonIgnoreIfNull]
        public double? HeightInMeters { get; set; }

        [BsonElement("lengthInFeet")]
        [BsonIgnoreIfNull]
        public double? LengthInFeet { get; set; }

        [BsonElement("lengthInMeters")]
        [BsonIgnoreIfNull]
        public double? LengthInMeters { get; set; }

        [BsonElement("speedInMph")]
        [BsonIgnoreIfNull]
        public double? SpeedInMph { get; set; }

        [BsonElement("speedInKmH")]
        [BsonIgnoreIfNull]
        public double? SpeedInKmH { get; set; }

        [BsonElement("dropInMeters")]
        [BsonIgnoreIfNull]
        public double? DropInMeters { get; set; }

        [BsonElement("inversionCount")]
        [BsonIgnoreIfNull]
        public int? InversionCount { get; set; }

        [BsonElement("openingDateText")]
        [BsonIgnoreIfNull]
        public string? OpeningDateText { get; set; }

        [BsonElement("closingDateText")]
        [BsonIgnoreIfNull]
        public string? ClosingDateText { get; set; }

        [BsonElement("openingDate")]
        [BsonIgnoreIfNull]
        public DateTime? OpeningDate { get; set; }

        [BsonElement("closingDate")]
        [BsonIgnoreIfNull]
        public DateTime? ClosingDate { get; set; }

        [BsonElement("status")]
        [BsonIgnoreIfNull]
        public string? Status { get; set; }

        [BsonElement("scrapedAtUtc")]
        [BsonIgnoreIfNull]
        public DateTime? ScrapedAtUtc { get; set; }

        [BsonElement("rawAttributes")]
        [BsonIgnoreIfNull]
        public Dictionary<string, string>? RawAttributes { get; set; }
    }
}
