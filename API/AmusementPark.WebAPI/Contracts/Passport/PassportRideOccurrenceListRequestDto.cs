using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportRideOccurrenceListRequestDto
{
    public int Limit { get; init; } = 100;

    public string? Cursor { get; init; }
}
