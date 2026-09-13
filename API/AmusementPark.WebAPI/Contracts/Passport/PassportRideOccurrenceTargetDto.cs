using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportRideOccurrenceTargetDto
{
    public string Name { get; init; } = string.Empty;

    public string? Category { get; init; }

    public string? LifecycleStatus { get; init; }

    public bool IsHistoricalSnapshot { get; init; }

    public DateOnly? OpeningDate { get; init; }

    public DateOnly? ClosingDate { get; init; }
}
