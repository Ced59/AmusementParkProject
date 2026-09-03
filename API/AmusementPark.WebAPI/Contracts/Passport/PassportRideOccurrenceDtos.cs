using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportRideOccurrenceStatusDto
{
    Completed = 1,
    Attempted = 2,
    MissedClosed = 3,
    MissedUnavailable = 4,
    SkippedByChoice = 5,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportRideLogSourceDto
{
    Manual = 1,
    Import = 2,
    SystemMigration = 3,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportHistoricalConsistencyDto
{
    Verified = 1,
    Unverified = 2,
    ConfirmedConflict = 3,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassportRideOccurrencePlacementDto
{
    First = 1,
    Last = 2,
    Before = 3,
    After = 4,
}

public sealed class PassportRideOccurrenceMomentDto
{
    public TimeOnly? LocalTime { get; init; }

    public bool IsApproximate { get; init; }
}

public sealed class CreatePassportRideOccurrenceRequestDto
{
    public string ParkItemId { get; init; } = string.Empty;

    public PassportRideOccurrenceMomentDto Moment { get; init; } =
        new PassportRideOccurrenceMomentDto();

    public PassportRideOccurrenceStatusDto Status { get; init; } =
        PassportRideOccurrenceStatusDto.Completed;

    public string? PrivateNote { get; init; }

    public bool ConfirmHistoricalConflict { get; init; }
}

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

public sealed class CreatePassportRideOccurrencesBatchRequestDto
{
    public IReadOnlyCollection<CreatePassportRideOccurrenceBatchItemDto> Items { get; init; } =
        Array.Empty<CreatePassportRideOccurrenceBatchItemDto>();
}

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

public sealed class ReorderPassportRideOccurrenceRequestDto
{
    public string OccurrenceId { get; init; } = string.Empty;

    public long ExpectedVersion { get; init; }

    public string? AnchorOccurrenceId { get; init; }

    public PassportRideOccurrencePlacementDto Placement { get; init; }
}

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

    public string? PrivateNote { get; init; }

    public bool CountsAsRide { get; init; }

    public long Version { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class PassportRideOccurrenceListRequestDto
{
    public int Limit { get; init; } = 100;

    public string? Cursor { get; init; }
}

public sealed class PassportRideOccurrencePageDto
{
    public IReadOnlyCollection<PassportRideOccurrenceDto> Items { get; init; } =
        Array.Empty<PassportRideOccurrenceDto>();

    public string? NextCursor { get; init; }
}
