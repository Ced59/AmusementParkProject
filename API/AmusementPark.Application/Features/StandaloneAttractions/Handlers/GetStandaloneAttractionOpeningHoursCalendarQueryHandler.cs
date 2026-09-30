using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Results;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
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
