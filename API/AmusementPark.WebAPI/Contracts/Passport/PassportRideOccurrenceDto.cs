using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportRideOccurrenceDto
{
    public string Id { get; init; } = string.Empty;

    public string VisitId { get; init; } = string.Empty;

    public string ParkId { get; init; } = string.Empty;

    public string ParkItemId { get; init; } = string.Empty;

    public long SortPosition { get; init; }

    public PassportRideOccurrenceMomentDto Moment { get; init; } =
        new PassportRideOccurrenceMomentDto();

    public PassportRideOccurrenceStatusDto Status { get; init; }

    public PassportRideLogSourceDto Source { get; init; }

    public PassportHistoricalConsistencyDto HistoricalConsistency { get; init; }

    public bool HistoricalConflictConfirmed { get; init; }

    public string? PrivateNote { get; init; }

    public bool CountsAsRide { get; init; }

    public long Version { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public PassportRideAssessmentDto? Assessment { get; init; }

    public PassportRideOccurrenceTargetDto? Target { get; init; }
}
