using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportVisitPageDto
{
    public IReadOnlyCollection<PassportVisitDto> Items { get; init; } =
        Array.Empty<PassportVisitDto>();

    public string? NextCursor { get; init; }
}
