using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportRideOccurrencePlacementDto
{
    First = 1,
    Last = 2,
    Before = 3,
    After = 4,
}
