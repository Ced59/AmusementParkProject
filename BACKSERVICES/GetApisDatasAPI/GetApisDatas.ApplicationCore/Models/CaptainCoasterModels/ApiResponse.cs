using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GetApisDatas.ApplicationCore.Models.CaptainCoasterModels
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("hydra:member")]
        public List<T>? HydraMember { get; set; }

        [JsonPropertyName("hydra:view")]
        public HydraView? HydraView { get; set; }
    }
}