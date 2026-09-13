using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportVisitPrivacyDto
{
    Private = 1,
    Unlisted = 2,
    Public = 3,
}
