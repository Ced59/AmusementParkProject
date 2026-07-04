using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Results;

public sealed class ParkListResult
{
    public required Park Park { get; init; }

    public int? ParkItemsTotalCount { get; init; }

    public int? ParkItemsVisibleCount { get; init; }

    public ParkOpeningHoursAdminSummaryResult? OpeningHours { get; init; }

    public DataCompletenessScore? DataCompleteness { get; init; }
}

public sealed class ParkOpeningHoursAdminSummaryResult
{
    public bool HasOpeningHours { get; init; }

    public ParkOpeningHoursAdminStatus Status { get; init; } = ParkOpeningHoursAdminStatus.NotConfigured;

    public string? TimeZoneId { get; init; }

    public DateOnly? FirstDate { get; init; }

    public DateOnly? LastDate { get; init; }

    public DateOnly? CompleteUntilDate { get; init; }

    public int? CompleteForDays { get; init; }

    public int WarningThresholdDays { get; init; } = 30;

    public DateTime? LastVerifiedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}
