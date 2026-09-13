using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportRideOccurrenceStatusDto
{
    Completed = 1,
    Attempted = 2,
    MissedClosed = 3,
    MissedUnavailable = 4,
    SkippedByChoice = 5,
}
