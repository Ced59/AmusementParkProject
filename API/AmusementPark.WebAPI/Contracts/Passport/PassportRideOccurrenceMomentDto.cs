using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportRideOccurrenceMomentDto
{
    public TimeOnly? LocalTime { get; init; }

    public bool IsApproximate { get; init; }
}
