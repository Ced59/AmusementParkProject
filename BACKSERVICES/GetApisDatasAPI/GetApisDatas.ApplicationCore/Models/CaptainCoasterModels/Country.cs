using System.Text.Json.Serialization;

namespace Models
{
    public class Country
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}