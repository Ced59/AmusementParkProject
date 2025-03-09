using System.Text.Json.Serialization;

namespace Models
{
    public class Park
    {
        [JsonPropertyName("id")]
        public int CaptainCoasterId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("country")]
        public Country? Country { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }
}