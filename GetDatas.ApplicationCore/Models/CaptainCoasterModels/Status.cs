using System.Text.Json.Serialization;

namespace Models
{
    public class Status
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}