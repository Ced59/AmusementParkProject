using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Results;

namespace AmusementPark.Application.Features.StandaloneAttractions.Queries;

public sealed record GetStandaloneAttractionOpeningHoursCalendarQuery(
    string StandaloneAttractionId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    bool IncludeHidden) : IQuery<ApplicationResult<ParkOpeningHoursCalendarResult>>;
