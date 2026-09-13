using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportVisitStatusDto
{
    Draft = 1,
    Completed = 2,
    Archived = 3,
}
