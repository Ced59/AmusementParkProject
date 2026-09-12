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

        if (!query.IncludeHidden && !park.Status.CanHaveCurrentOpeningHours())
        {
            return ApplicationResult<ParkOpeningHoursScheduleResult>.Failure(ParkOpeningHoursApplicationErrors.ScheduleNotFound());
        }

        ParkOpeningHoursSchedule? schedule = await this.openingHoursRepository.GetByParkIdAsync(parkId, cancellationToken);
        if (schedule is null)
        {
            return ApplicationResult<ParkOpeningHoursScheduleResult>.Failure(ParkOpeningHoursApplicationErrors.ScheduleNotFound());
        }

        return ApplicationResult<ParkOpeningHoursScheduleResult>.Success(schedule.ToScheduleResult());
    }
}
