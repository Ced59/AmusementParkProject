using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Disponibilité d'un parc liée à la date exacte qui a été évaluée.
/// </summary>
public sealed class ParkFitDateAvailability
{
    public ParkFitDateAvailability(
        ParkFitDateAvailabilityState state,
        DateOnly evaluationDate,
        ParkFitCalendarState? calendarState = null,
        IReadOnlyCollection<ParkOpeningHoursTimeRange>? timeRanges = null,
        string? timeZoneId = null,
        string? sourceUrl = null,
        DateTime? lastVerifiedAtUtc = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        if (calendarState.HasValue && !Enum.IsDefined(calendarState.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(calendarState));
        }

        if (lastVerifiedAtUtc.HasValue && lastVerifiedAtUtc.Value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The calendar verification timestamp must use UTC.",
                nameof(lastVerifiedAtUtc));
        }

        this.State = state;
        this.EvaluationDate = evaluationDate;
        this.CalendarState = calendarState ?? state switch
        {
            ParkFitDateAvailabilityState.Available => ParkFitCalendarState.OpenConfirmed,
            ParkFitDateAvailabilityState.Unavailable => ParkFitCalendarState.ClosedConfirmed,
            _ => ParkFitCalendarState.CalendarNotPublished,
        };
        this.TimeRanges = timeRanges is null
            ? Array.Empty<ParkOpeningHoursTimeRange>()
            : timeRanges.ToList();
        this.TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? null : timeZoneId.Trim();
        this.SourceUrl = string.IsNullOrWhiteSpace(sourceUrl) ? null : sourceUrl.Trim();
        this.LastVerifiedAtUtc = lastVerifiedAtUtc;
    }

    public ParkFitDateAvailabilityState State { get; }

    public DateOnly EvaluationDate { get; }

    public ParkFitCalendarState CalendarState { get; }

    public IReadOnlyCollection<ParkOpeningHoursTimeRange> TimeRanges { get; }

    public string? TimeZoneId { get; }

    public string? SourceUrl { get; }

    public DateTime? LastVerifiedAtUtc { get; }
}
