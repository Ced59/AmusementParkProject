using System.Text.Json.Serialization;

namespace GetApisDatas.ApplicationCore.Models.CaptainCoasterModels
{
    public class Status
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}