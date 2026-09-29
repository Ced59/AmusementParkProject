using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Commands;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Handlers;

public sealed class UpsertStandaloneAttractionOpeningHoursCommandHandler :
    ICommandHandler<UpsertStandaloneAttractionOpeningHoursCommand, ApplicationResult<ParkOpeningHoursSchedule>>
{
    private readonly IStandaloneAttractionRepository attractionRepository;
    private readonly IStandaloneAttractionOpeningHoursRepository openingHoursRepository;
    private readonly ParkOpeningHoursScheduleNormalizer normalizer;
    private readonly ParkOpeningHoursCoverageSegmentBuilder coverageSegmentBuilder;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;

    public UpsertStandaloneAttractionOpeningHoursCommandHandler(
        IStandaloneAttractionRepository attractionRepository,
        IStandaloneAttractionOpeningHoursRepository openingHoursRepository,
        ParkOpeningHoursScheduleNormalizer normalizer,
        ParkOpeningHoursCoverageSegmentBuilder coverageSegmentBuilder,
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
    {
        this.attractionRepository = attractionRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.normalizer = normalizer;
        this.coverageSegmentBuilder = coverageSegmentBuilder;
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
    }

    public async Task<ApplicationResult<ParkOpeningHoursSchedule>> HandleAsync(
        UpsertStandaloneAttractionOpeningHoursCommand command,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkOpeningHoursSchedule> normalizedResult =
            this.normalizer.Normalize(command.Schedule);
        if (!normalizedResult.IsSuccess || normalizedResult.Value is null)
        {
            return normalizedResult;
        }

        ParkOpeningHoursSchedule schedule = normalizedResult.Value;
        StandaloneAttraction? attraction = await this.attractionRepository.GetByIdAsync(
            schedule.ParkId,
            includeHidden: true,
            cancellationToken);
        if (attraction is null)
        {
            return ApplicationResult<ParkOpeningHoursSchedule>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.AttractionNotFound());
        }

        if (!ParkItemStatusNormalizer.IsOperating(attraction.AttractionDetails?.Status))
        {
            return ApplicationResult<ParkOpeningHoursSchedule>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.CurrentInformationNotAllowed());
        }

        schedule.CoverageSegments = this.coverageSegmentBuilder.BuildSegments(schedule).ToList();
        ParkOpeningHoursSchedule savedSchedule =
            await this.openingHoursRepository.UpsertAsync(schedule, cancellationToken);
        await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        return ApplicationResult<ParkOpeningHoursSchedule>.Success(savedSchedule);
    }
}
