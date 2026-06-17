using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Videos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VideoOwnerTypeDto
{
    NONE = 0,
    PARK = 1,
    ATTRACTION = 2,
}
