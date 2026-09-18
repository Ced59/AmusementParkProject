using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Trips;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TripDelegatedRoleDto
{
    Editor = 1,
    Participant = 2,
    Viewer = 3,
}
