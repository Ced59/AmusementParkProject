namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkOpeningHoursCalendar
{
    public string ParkId { get; init; } = string.Empty;

    public string TimeZoneId { get; init; } = string.Empty;

    public string? SourceUrl { get; init; }

    public string? Notes { get; init; }

    public DateTime? LastVerifiedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public DateOnly? FirstDate { get; init; }

    public DateOnly? LastDate { get; init; }

    public DateOnly FromDate { get; init; }

    public DateOnly ToDate { get; init; }

    public IReadOnlyCollection<ParkOpeningHoursDay> Days { get; init; } = Array.Empty<ParkOpeningHoursDay>();
}
