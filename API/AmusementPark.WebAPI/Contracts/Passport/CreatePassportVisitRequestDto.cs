using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class CreatePassportVisitRequestDto
{
    public string ParkId { get; init; } = string.Empty;

    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();

    public string? TimeZoneId { get; init; }

    public PassportLocalServiceDayConventionDto ServiceDayConvention { get; init; } =
        PassportLocalServiceDayConventionDto.VisitStartLocalDate;

    public string? Title { get; init; }

    public string? PrivateNote { get; init; }
}
