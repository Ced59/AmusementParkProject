using System.Text.Json.Serialization;

namespace GetApisDatas.ApplicationCore.Models.CaptainCoasterModels
{
    public class HydraView
    {
        [JsonPropertyName("hydra:next")]
        public string? Next { get; set; }
    }
}