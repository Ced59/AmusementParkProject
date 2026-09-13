using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class CreatePassportRideOccurrenceBatchItemDto
{
    public string ParkItemId { get; init; } = string.Empty;

    public PassportRideOccurrenceMomentDto Moment { get; init; } =
        new PassportRideOccurrenceMomentDto();

    public PassportRideOccurrenceStatusDto Status { get; init; } =
        PassportRideOccurrenceStatusDto.Completed;

    public string? PrivateNote { get; init; }

    public bool ConfirmHistoricalConflict { get; init; }

    public int Count { get; init; } = 1;
}
