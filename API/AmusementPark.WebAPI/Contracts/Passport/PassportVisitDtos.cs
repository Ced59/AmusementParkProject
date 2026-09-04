using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportVisitDatePrecisionDto
{
    Year = 1,
    Month = 2,
    Day = 3,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportLocalServiceDayConventionDto
{
    VisitStartLocalDate = 1,
    UserSelectedServiceDate = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportVisitStatusDto
{
    Draft = 1,
    Completed = 2,
    Archived = 3,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportVisitPrivacyDto
{
    Private = 1,
    Unlisted = 2,
    Public = 3,
}

public sealed class PassportVisitDateDto
{
    public int Year { get; init; }

    public int? Month { get; init; }

    public int? Day { get; init; }

    public PassportVisitDatePrecisionDto Precision { get; init; }

    public bool IsApproximate { get; init; }
}

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

public sealed class UpdatePassportVisitRequestDto
{
    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();

    public string? TimeZoneId { get; init; }

    public PassportLocalServiceDayConventionDto ServiceDayConvention { get; init; } =
        PassportLocalServiceDayConventionDto.VisitStartLocalDate;

    public string? Title { get; init; }

    public string? PrivateNote { get; init; }

    public long ExpectedVersion { get; init; }
}

public sealed class MutatePassportVisitStatusRequestDto
{
    public long ExpectedVersion { get; init; }
}

public sealed class UpsertPassportVisitParkAssessmentRequestDto
{
    public double Value { get; init; }

    public string? PrivateComment { get; init; }

    public long ExpectedVersion { get; init; }
}

public sealed class PassportVisitParkAssessmentDto
{
    public double Value { get; init; }

    public string? PrivateComment { get; init; }

    public int Revision { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class PassportVisitDto
{
    public string Id { get; init; } = string.Empty;

    public string ParkId { get; init; } = string.Empty;

    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();

    public string? TimeZoneId { get; init; }

    public PassportLocalServiceDayConventionDto ServiceDayConvention { get; init; }

    public PassportVisitStatusDto Status { get; init; }

    public PassportVisitPrivacyDto Privacy { get; init; }

    public string? Title { get; init; }

    public string? PrivateNote { get; init; }

    public PassportVisitParkAssessmentDto? ParkAssessment { get; init; }

    public long Version { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }
}

public sealed class PassportVisitListRequestDto
{
    public int Limit { get; init; } = 25;

    public string? ParkId { get; init; }

    public int? Year { get; init; }

    public PassportVisitStatusDto? Status { get; init; }

    public string? Cursor { get; init; }
}

public sealed class PassportVisitPageDto
{
    public IReadOnlyCollection<PassportVisitDto> Items { get; init; } =
        Array.Empty<PassportVisitDto>();

    public string? NextCursor { get; init; }
}
