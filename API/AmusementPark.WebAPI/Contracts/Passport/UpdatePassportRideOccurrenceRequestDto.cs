using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class UpdatePassportRideOccurrenceRequestDto
{
    [Range(1, long.MaxValue)]
    public long ExpectedVersion { get; init; }

    public PassportRideOccurrenceMomentDto Moment { get; init; } =
        new PassportRideOccurrenceMomentDto();

    [EnumDataType(typeof(PassportRideOccurrenceStatusDto))]
    public PassportRideOccurrenceStatusDto Status { get; init; }

    public string? PrivateNote { get; init; }

    public bool ConfirmHistoricalConflict { get; init; }
}
