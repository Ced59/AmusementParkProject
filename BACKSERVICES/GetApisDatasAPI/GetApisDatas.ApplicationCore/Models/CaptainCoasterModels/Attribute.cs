using System.Text.Json.Serialization;

namespace GetApisDatas.ApplicationCore.Models.CaptainCoasterModels
{
    public class Attribute
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
