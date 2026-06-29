using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Commands;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Handlers;

public sealed class UpsertParkOpeningHoursScheduleCommandHandler : ICommandHandler<UpsertParkOpeningHoursScheduleCommand, ApplicationResult<ParkOpeningHoursSchedule>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkOpeningHoursRepository openingHoursRepository;
    private readonly ParkOpeningHoursScheduleNormalizer normalizer;
    private readonly ParkOpeningHoursCoverageSegmentBuilder coverageSegmentBuilder;

    public UpsertParkOpeningHoursScheduleCommandHandler(
        IParkRepository parkRepository,
        IParkOpeningHoursRepository openingHoursRepository,
        ParkOpeningHoursScheduleNormalizer normalizer,
        ParkOpeningHoursCoverageSegmentBuilder coverageSegmentBuilder)
    {
        this.parkRepository = parkRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.normalizer = normalizer;
        this.coverageSegmentBuilder = coverageSegmentBuilder;
    }

    public async Task<ApplicationResult<ParkOpeningHoursSchedule>> HandleAsync(UpsertParkOpeningHoursScheduleCommand command, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkOpeningHoursSchedule> normalizedResult = this.normalizer.Normalize(command.Schedule);
        if (!normalizedResult.IsSuccess || normalizedResult.Value is null)
        {
            return normalizedResult;
        }

        ParkOpeningHoursSchedule normalizedSchedule = normalizedResult.Value;
        Park? park = await this.parkRepository.GetByIdAsync(normalizedSchedule.ParkId, includeHidden: true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkOpeningHoursSchedule>.Failure(ParkOpeningHoursApplicationErrors.ParkNotFound());
        }

        normalizedSchedule.CoverageSegments = this.coverageSegmentBuilder.BuildSegments(normalizedSchedule).ToList();
        ParkOpeningHoursSchedule savedSchedule = await this.openingHoursRepository.UpsertAsync(normalizedSchedule, cancellationToken);
        return ApplicationResult<ParkOpeningHoursSchedule>.Success(savedSchedule);
    }
}
