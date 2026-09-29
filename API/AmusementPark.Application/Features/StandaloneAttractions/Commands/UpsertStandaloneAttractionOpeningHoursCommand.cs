using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Commands;

public sealed record UpsertStandaloneAttractionOpeningHoursCommand(
    ParkOpeningHoursSchedule Schedule) : ICommand<ApplicationResult<ParkOpeningHoursSchedule>>;
