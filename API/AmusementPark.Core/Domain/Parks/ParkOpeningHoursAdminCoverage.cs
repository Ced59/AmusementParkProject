namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkOpeningHoursAdminCoverage
{
    public ParkOpeningHoursAdminStatus Status { get; init; } = ParkOpeningHoursAdminStatus.NotConfigured;

    public int? CompleteForDays { get; init; }

    public DateOnly? CompleteUntilDate { get; init; }

    public int WarningThresholdDays { get; init; } = 30;
}
