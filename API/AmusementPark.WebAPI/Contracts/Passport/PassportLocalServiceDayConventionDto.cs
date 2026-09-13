using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportLocalServiceDayConventionDto
{
    VisitStartLocalDate = 1,
    UserSelectedServiceDate = 2,
}
