using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Results;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Commands;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Handlers;

public sealed class GetStandaloneAttractionOpeningHoursCalendarQueryHandler :
    IQueryHandler<GetStandaloneAttractionOpeningHoursCalendarQuery, ApplicationResult<ParkOpeningHoursCalendarResult>>
{
    private readonly IStandaloneAttractionRepository attractionRepository;
    private readonly IStandaloneAttractionOpeningHoursRepository openingHoursRepository;
    private readonly ParkOpeningHoursCalendarBuilder calendarBuilder;

    public GetStandaloneAttractionOpeningHoursCalendarQueryHandler(
        IStandaloneAttractionRepository attractionRepository,
        IStandaloneAttractionOpeningHoursRepository openingHoursRepository,
        ParkOpeningHoursCalendarBuilder calendarBuilder)
    {
        this.attractionRepository = attractionRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.calendarBuilder = calendarBuilder;
    }

    public async Task<ApplicationResult<ParkOpeningHoursCalendarResult>> HandleAsync(
        GetStandaloneAttractionOpeningHoursCalendarQuery query,
        CancellationToken cancellationToken = default)
    {
        string attractionId = (query.StandaloneAttractionId ?? string.Empty).Trim();
        StandaloneAttraction? attraction = attractionId.Length == 0
            ? null
            : await this.attractionRepository.GetByIdAsync(
                attractionId,
                query.IncludeHidden,
                cancellationToken);
        if (attraction is null)
        {
            return ApplicationResult<ParkOpeningHoursCalendarResult>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.AttractionNotFound());
        }

        if (!query.IncludeHidden
            && !ParkItemStatusNormalizer.IsOperating(attraction.AttractionDetails?.Status))
        {
            return ApplicationResult<ParkOpeningHoursCalendarResult>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.OpeningHoursNotFound());
        }

        ParkOpeningHoursSchedule? schedule =
            await this.openingHoursRepository.GetByStandaloneAttractionIdAsync(
                attractionId,
                cancellationToken);
        if (schedule is null || (schedule.RegularRules.Count == 0 && schedule.DateOverrides.Count == 0))
        {
            return ApplicationResult<ParkOpeningHoursCalendarResult>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.OpeningHoursNotFound());
        }

        ParkOpeningHoursCalendar calendar = this.calendarBuilder.BuildCalendar(
            schedule,
            query.FromDate,
            query.ToDate);
        return ApplicationResult<ParkOpeningHoursCalendarResult>.Success(calendar.ToCalendarResult());
    }
}

public sealed class GetStandaloneAttractionOpeningHoursScheduleQueryHandler :
    IQueryHandler<GetStandaloneAttractionOpeningHoursScheduleQuery, ApplicationResult<ParkOpeningHoursScheduleResult>>
{
    private readonly IStandaloneAttractionRepository attractionRepository;
    private readonly IStandaloneAttractionOpeningHoursRepository openingHoursRepository;

    public GetStandaloneAttractionOpeningHoursScheduleQueryHandler(
        IStandaloneAttractionRepository attractionRepository,
        IStandaloneAttractionOpeningHoursRepository openingHoursRepository)
    {
        this.attractionRepository = attractionRepository;
        this.openingHoursRepository = openingHoursRepository;
    }

    public async Task<ApplicationResult<ParkOpeningHoursScheduleResult>> HandleAsync(
        GetStandaloneAttractionOpeningHoursScheduleQuery query,
        CancellationToken cancellationToken = default)
    {
        string attractionId = (query.StandaloneAttractionId ?? string.Empty).Trim();
        StandaloneAttraction? attraction = attractionId.Length == 0
            ? null
            : await this.attractionRepository.GetByIdAsync(
                attractionId,
                query.IncludeHidden,
                cancellationToken);
        if (attraction is null)
        {
            return ApplicationResult<ParkOpeningHoursScheduleResult>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.AttractionNotFound());
        }

        if (!query.IncludeHidden
            && !ParkItemStatusNormalizer.IsOperating(attraction.AttractionDetails?.Status))
        {
            return ApplicationResult<ParkOpeningHoursScheduleResult>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.OpeningHoursNotFound());
        }

        ParkOpeningHoursSchedule? schedule =
            await this.openingHoursRepository.GetByStandaloneAttractionIdAsync(
                attractionId,
                cancellationToken);
        return schedule is null
            ? ApplicationResult<ParkOpeningHoursScheduleResult>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.OpeningHoursNotFound())
            : ApplicationResult<ParkOpeningHoursScheduleResult>.Success(schedule.ToScheduleResult());
    }
}

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
