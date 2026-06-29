using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Queries;
using AmusementPark.Application.Features.ParkOpeningHours.Results;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Handlers;

public sealed class GetParkOpeningHoursScheduleQueryHandler : IQueryHandler<GetParkOpeningHoursScheduleQuery, ApplicationResult<ParkOpeningHoursScheduleResult>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkOpeningHoursRepository openingHoursRepository;

    public GetParkOpeningHoursScheduleQueryHandler(
        IParkRepository parkRepository,
        IParkOpeningHoursRepository openingHoursRepository)
    {
        this.parkRepository = parkRepository;
        this.openingHoursRepository = openingHoursRepository;
    }

    public async Task<ApplicationResult<ParkOpeningHoursScheduleResult>> HandleAsync(GetParkOpeningHoursScheduleQuery query, CancellationToken cancellationToken = default)
    {
        string parkId = (query.ParkId ?? string.Empty).Trim();
        if (parkId.Length == 0)
        {
            return ApplicationResult<ParkOpeningHoursScheduleResult>.Failure(ParkOpeningHoursApplicationErrors.ParkNotFound());
        }

        Park? park = await this.parkRepository.GetByIdAsync(parkId, query.IncludeHidden, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkOpeningHoursScheduleResult>.Failure(ParkOpeningHoursApplicationErrors.ParkNotFound());
        }

        ParkOpeningHoursSchedule? schedule = await this.openingHoursRepository.GetByParkIdAsync(parkId, cancellationToken);
        if (schedule is null)
        {
            return ApplicationResult<ParkOpeningHoursScheduleResult>.Failure(ParkOpeningHoursApplicationErrors.ScheduleNotFound());
        }

        return ApplicationResult<ParkOpeningHoursScheduleResult>.Success(schedule.ToScheduleResult());
    }
}

public sealed class GetParkOpeningHoursCalendarQueryHandler : IQueryHandler<GetParkOpeningHoursCalendarQuery, ApplicationResult<ParkOpeningHoursCalendarResult>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkOpeningHoursRepository openingHoursRepository;
    private readonly ParkOpeningHoursCalendarBuilder calendarBuilder;

    public GetParkOpeningHoursCalendarQueryHandler(
        IParkRepository parkRepository,
        IParkOpeningHoursRepository openingHoursRepository,
        ParkOpeningHoursCalendarBuilder calendarBuilder)
    {
        this.parkRepository = parkRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.calendarBuilder = calendarBuilder;
    }

    public async Task<ApplicationResult<ParkOpeningHoursCalendarResult>> HandleAsync(GetParkOpeningHoursCalendarQuery query, CancellationToken cancellationToken = default)
    {
        string parkId = (query.ParkId ?? string.Empty).Trim();
        if (parkId.Length == 0)
        {
            return ApplicationResult<ParkOpeningHoursCalendarResult>.Failure(ParkOpeningHoursApplicationErrors.ParkNotFound());
        }

        Park? park = await this.parkRepository.GetByIdAsync(parkId, query.IncludeHidden, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkOpeningHoursCalendarResult>.Failure(ParkOpeningHoursApplicationErrors.ParkNotFound());
        }

        ParkOpeningHoursSchedule? schedule = await this.openingHoursRepository.GetByParkIdAsync(parkId, cancellationToken);
        if (schedule is null || (schedule.RegularRules.Count == 0 && schedule.DateOverrides.Count == 0))
        {
            return ApplicationResult<ParkOpeningHoursCalendarResult>.Failure(ParkOpeningHoursApplicationErrors.ScheduleNotFound());
        }

        ParkOpeningHoursCalendarResult calendar = this.calendarBuilder.BuildCalendar(schedule, query.FromDate, query.ToDate);
        return ApplicationResult<ParkOpeningHoursCalendarResult>.Success(calendar);
    }
}
