using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Results;

namespace AmusementPark.Application.Features.ParkOpeningHours.Queries;

public sealed record GetParkOpeningHoursScheduleQuery(string ParkId, bool IncludeHidden) : IQuery<ApplicationResult<ParkOpeningHoursScheduleResult>>;

public sealed record GetParkOpeningHoursCalendarQuery(string ParkId, DateOnly? FromDate, DateOnly? ToDate, bool IncludeHidden) : IQuery<ApplicationResult<ParkOpeningHoursCalendarResult>>;
