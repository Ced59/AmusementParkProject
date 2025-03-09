using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Models
{
    public class Coaster
    {
        [JsonPropertyName("@id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("materialType")]
        public Attribute? MaterialType { get; set; }

        [JsonPropertyName("seatingType")]
        public Attribute? SeatingType { get; set; }

        [JsonPropertyName("model")]
        public Attribute? Model { get; set; }

        [JsonPropertyName("speed")]
        public double? Speed { get; set; }

        [JsonPropertyName("height")]
        public double? Height { get; set; }

        [JsonPropertyName("length")]
        public double? Length { get; set; }

        [JsonPropertyName("inversionsNumber")]
        public int? InversionsNumber { get; set; }

        [JsonPropertyName("manufacturer")]
        public Attribute? Manufacturer { get; set; }

        [JsonPropertyName("restraint")]
        public Attribute? Restraint { get; set; }

        [JsonPropertyName("launchs")]
        public List<Attribute>? Launchs { get; set; }

        [JsonPropertyName("park")]
        public Park? Park { get; set; }

        [JsonPropertyName("status")]
        public Attribute? Status { get; set; }

        [JsonPropertyName("openingDate")]
        public DateTime? OpeningDate { get; set; }

        [JsonPropertyName("closingDate")]
        public DateTime? ClosingDate { get; set; }

        [JsonPropertyName("totalRatings")]
        public int? TotalRatings { get; set; }

        [JsonPropertyName("validDuels")]
        public int? ValidDuels { get; set; }

        [JsonPropertyName("score")]
        public string? Score { get; set; }

        [JsonPropertyName("rank")]
        public int? Rank { get; set; }

        [JsonPropertyName("mainImage")]
        public Image? MainImage { get; set; }
    }
}
