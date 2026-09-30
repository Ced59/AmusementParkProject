using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Results;

namespace AmusementPark.Application.Features.StandaloneAttractions.Queries;

public sealed record GetStandaloneAttractionOpeningHoursScheduleQuery(
    string StandaloneAttractionId,
    bool IncludeHidden) : IQuery<ApplicationResult<ParkOpeningHoursScheduleResult>>;
