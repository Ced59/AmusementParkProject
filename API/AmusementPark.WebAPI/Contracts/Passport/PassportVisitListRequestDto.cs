using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportVisitListRequestDto
{
    public int Limit { get; init; } = 25;

    public string? ParkId { get; init; }

    public int? Year { get; init; }

    public PassportVisitStatusDto? Status { get; init; }

    public string? Cursor { get; init; }
}
