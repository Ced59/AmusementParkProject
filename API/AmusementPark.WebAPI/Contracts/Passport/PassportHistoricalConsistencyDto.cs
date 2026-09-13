using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportHistoricalConsistencyDto
{
    Verified = 1,
    Unverified = 2,
    ConfirmedConflict = 3,
}
