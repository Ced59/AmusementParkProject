using System.Text.Json.Serialization;

namespace Models
{
    public class HydraView
    {
        [JsonPropertyName("hydra:next")]
        public string? Next { get; set; }
    }
}