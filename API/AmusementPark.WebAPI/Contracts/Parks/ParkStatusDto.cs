using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Parks;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ParkStatusDto
{
    Operating = 0,
    ClosedDefinitively = 1,
    Planned = 2,
    UnderConstruction = 3,
    TemporarilyClosed = 4,
    Cancelled = 5,
}
