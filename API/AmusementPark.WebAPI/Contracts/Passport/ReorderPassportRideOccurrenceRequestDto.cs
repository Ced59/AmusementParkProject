using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class ReorderPassportRideOccurrenceRequestDto
{
    public string OccurrenceId { get; init; } = string.Empty;

    public long ExpectedVersion { get; init; }

    public string? AnchorOccurrenceId { get; init; }

    public PassportRideOccurrencePlacementDto Placement { get; init; }
}
