using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportVisitDto
{
    public string Id { get; init; } = string.Empty;

    public string ParkId { get; init; } = string.Empty;

    public string? ParkName { get; init; }

    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();

    public string? TimeZoneId { get; init; }

    public PassportLocalServiceDayConventionDto ServiceDayConvention { get; init; }

    public PassportVisitStatusDto Status { get; init; }

    public PassportVisitPrivacyDto Privacy { get; init; }

    public string? Title { get; init; }

    public string? PrivateNote { get; init; }

    public bool HasPrivateNote { get; init; }

    public PassportVisitParkAssessmentDto? ParkAssessment { get; init; }

    public long Version { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }
}
