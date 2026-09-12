using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Commands;

public sealed record UpsertParkOpeningHoursScheduleCommand(ParkOpeningHoursSchedule Schedule) : ICommand<ApplicationResult<ParkOpeningHoursSchedule>>;
