using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Results;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Handlers;

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
