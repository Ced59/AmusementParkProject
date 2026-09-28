using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.LiveData;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LiveMappingConfidenceDto
{
    Low = 1,
    Medium = 2,
    High = 3,
}
