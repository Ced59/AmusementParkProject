using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Trips;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TripInvitationPeriodKindDto
{
    Unspecified = 1,
    SingleMonth = 2,
    MonthRange = 3,
}
