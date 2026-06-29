namespace AmusementPark.Application.Features.ParkOpeningHours.Ports;

public sealed class ParkOpeningHoursCoverageNotification
{
    public string ParkId { get; init; } = string.Empty;

    public string ParkName { get; init; } = string.Empty;

    public int ThresholdDays { get; init; }

    public int CompleteForDays { get; init; }

    public int WarningThresholdDays { get; init; }

    public DateOnly? CompleteUntilDate { get; init; }

    public string TimeZoneId { get; init; } = string.Empty;

    public DateOnly LocalDate { get; init; }
}

public interface IParkOpeningHoursNotificationService
{
    Task NotifyCoverageThresholdReachedAsync(ParkOpeningHoursCoverageNotification notification, CancellationToken cancellationToken);
}
