using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Parks;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ParkTypeDto
{
    ThemePark,
    WaterPark,
    Zoo,
    AnimalPark,
    AmusementPark,
    Resort,
}
