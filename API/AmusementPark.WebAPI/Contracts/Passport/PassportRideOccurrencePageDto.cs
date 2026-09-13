using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportRideOccurrencePageDto
{
    public IReadOnlyCollection<PassportRideOccurrenceDto> Items { get; init; } =
        Array.Empty<PassportRideOccurrenceDto>();

    public string? NextCursor { get; init; }
}
