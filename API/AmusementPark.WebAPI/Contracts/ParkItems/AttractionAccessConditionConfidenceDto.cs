using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttractionAccessConditionConfidenceDto
{
    Unknown,
    Low,
    Medium,
    High,
}
