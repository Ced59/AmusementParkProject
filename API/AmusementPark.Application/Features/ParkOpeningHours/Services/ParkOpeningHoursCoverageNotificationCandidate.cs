using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Services;

internal sealed record ParkOpeningHoursCoverageNotificationCandidate(
    ParkOpeningHoursScheduleSummary Summary,
    ParkOpeningHoursAdminCoverage Coverage,
    int ThresholdDays,
    DateOnly LocalDate);
