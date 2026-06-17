using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Videos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VideoTypeDto
{
    OTHER = 0,
    ON_RIDE = 1,
    OFF_RIDE = 2,
    WALKTHROUGH = 3,
    ADVERTISEMENT = 4,
    DOCUMENTARY = 5,
    REVIEW = 6,
    NEWS = 7,
    EVENT = 8,
    INTERVIEW = 9,
}
