using System.Text.Json.Serialization;

namespace GetApisDatas.ApplicationCore.Models.CaptainCoasterModels
{
    public class Image
    {
        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }
}
