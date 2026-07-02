using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Parks;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ParkAudienceClassificationDto
{
    International,
    National,
    Regional,
    Local,
}
