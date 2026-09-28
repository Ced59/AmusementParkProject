using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.LiveData;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LiveTargetTypeDto
{
    Park = 1,
    ParkItem = 2,
}
