using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportRideLogSourceDto
{
    Manual = 1,
    Import = 2,
    SystemMigration = 3,
    TripTransition = 4,
}
