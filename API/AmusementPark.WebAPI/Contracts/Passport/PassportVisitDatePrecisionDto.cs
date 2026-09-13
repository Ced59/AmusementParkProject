using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportVisitDatePrecisionDto
{
    Year = 1,
    Month = 2,
    Day = 3,
}
