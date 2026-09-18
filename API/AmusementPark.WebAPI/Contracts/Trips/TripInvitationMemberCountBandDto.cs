using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Trips;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TripInvitationMemberCountBandDto
{
    One = 1,
    TwoToFive = 2,
    SixToTen = 3,
    ElevenToFifty = 4,
}
