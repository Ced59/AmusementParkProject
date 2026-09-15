using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Models;

public sealed record ParkOpeningHoursFactualWriteResult(
    ParkOpeningHoursSchedule Schedule,
    ParkOpeningHoursPendingFactualChange? PendingFactualChange);
