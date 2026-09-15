using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Commands;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkOpeningHours.Models;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Handlers;

public sealed class UpsertParkOpeningHoursScheduleCommandHandler : ICommandHandler<UpsertParkOpeningHoursScheduleCommand, ApplicationResult<ParkOpeningHoursSchedule>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkOpeningHoursRepository openingHoursRepository;
    private readonly ParkOpeningHoursScheduleNormalizer normalizer;
    private readonly ParkOpeningHoursCoverageSegmentBuilder coverageSegmentBuilder;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;
    private readonly IParkOpeningHoursFactualChangeCapture factualChangeCapture;

    public UpsertParkOpeningHoursScheduleCommandHandler(
        IParkRepository parkRepository,
        IParkOpeningHoursRepository openingHoursRepository,
        ParkOpeningHoursScheduleNormalizer normalizer,
        ParkOpeningHoursCoverageSegmentBuilder coverageSegmentBuilder,
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler,
        IParkOpeningHoursFactualChangeCapture factualChangeCapture)
    {
        this.parkRepository = parkRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.normalizer = normalizer;
        this.coverageSegmentBuilder = coverageSegmentBuilder;
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
        this.factualChangeCapture = factualChangeCapture;
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

        if (!park.Status.CanHaveCurrentOpeningHours())
        {
            return ApplicationResult<ParkOpeningHoursSchedule>.Failure(ParkOpeningHoursApplicationErrors.ScheduleNotAllowed(park.Status));
        }

        ParkOpeningHoursSchedule? previousSchedule =
            await this.openingHoursRepository.GetByParkIdAsync(
                normalizedSchedule.ParkId,
                cancellationToken);
        normalizedSchedule.CoverageSegments = this.coverageSegmentBuilder.BuildSegments(normalizedSchedule).ToList();
        ParkOpeningHoursFactualChangeDraft? factualChange =
            this.factualChangeCapture.Prepare(
                park,
                previousSchedule,
                normalizedSchedule);
        ParkOpeningHoursFactualWriteResult writeResult =
            await this.openingHoursRepository.UpsertWithFactualChangeAsync(
                normalizedSchedule,
                factualChange,
                cancellationToken);
        if (writeResult.PendingFactualChange is not null)
        {
            await this.factualChangeCapture.CaptureAsync(
                writeResult.PendingFactualChange,
                CancellationToken.None);
        }

        ParkOpeningHoursSchedule savedSchedule = writeResult.Schedule;
        await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        return ApplicationResult<ParkOpeningHoursSchedule>.Success(savedSchedule);
    }
}
