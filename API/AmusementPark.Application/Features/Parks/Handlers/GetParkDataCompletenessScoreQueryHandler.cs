using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Services;
using AmusementPark.Core.Domain.Parks;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.Parks.Handlers;

public sealed class GetParkDataCompletenessScoreQueryHandler : IQueryHandler<GetParkDataCompletenessScoreQuery, ApplicationResult<DataCompletenessScore>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkOpeningHoursRepository parkOpeningHoursRepository;
    private readonly ParkOpeningHoursAdminStatusResolver openingHoursStatusResolver;
    private readonly IParkZoneRepository? parkZoneRepository;
    private readonly IImageRepository? imageRepository;
    private readonly IHistoryEventRepository? historyEventRepository;
    private readonly IParkPricingRepository? parkPricingRepository;
    private readonly IParkFounderRepository? parkFounderRepository;
    private readonly IParkOperatorRepository? parkOperatorRepository;
    private readonly IAttractionManufacturerRepository? attractionManufacturerRepository;

    public GetParkDataCompletenessScoreQueryHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkOpeningHoursRepository parkOpeningHoursRepository,
        ParkOpeningHoursAdminStatusResolver openingHoursStatusResolver,
        IParkZoneRepository? parkZoneRepository = null,
        IImageRepository? imageRepository = null,
        IHistoryEventRepository? historyEventRepository = null,
        IParkPricingRepository? parkPricingRepository = null,
        IParkFounderRepository? parkFounderRepository = null,
        IParkOperatorRepository? parkOperatorRepository = null,
        IAttractionManufacturerRepository? attractionManufacturerRepository = null)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkOpeningHoursRepository = parkOpeningHoursRepository;
        this.openingHoursStatusResolver = openingHoursStatusResolver;
        this.parkZoneRepository = parkZoneRepository;
        this.imageRepository = imageRepository;
        this.historyEventRepository = historyEventRepository;
        this.parkPricingRepository = parkPricingRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
    }

    public async Task<ApplicationResult<DataCompletenessScore>> HandleAsync(GetParkDataCompletenessScoreQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<DataCompletenessScore>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        Park? park = await this.parkRepository.GetByIdAsync(query.ParkId.Trim(), query.IncludeHidden, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<DataCompletenessScore>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        List<string> parkIds = new List<string> { park.Id ?? query.ParkId.Trim() };
        IReadOnlyDictionary<string, ParkItemVisibilityCounts> countsByParkId = await this.parkItemRepository.GetVisibilityCountsByParkIdsAsync(parkIds, cancellationToken);
        IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary> openingHoursByParkId = await this.parkOpeningHoursRepository.GetSummariesByParkIdsAsync(parkIds, cancellationToken);
        IReadOnlyDictionary<string, ParkOpeningHoursSchedule> openingHoursSchedulesByParkId =
            (await this.parkOpeningHoursRepository.GetPublicTextByParkIdsAsync(parkIds, cancellationToken))
                .Where(static schedule => !string.IsNullOrWhiteSpace(schedule.ParkId))
                .GroupBy(static schedule => schedule.ParkId.Trim(), StringComparer.Ordinal)
                .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
        IReadOnlyDictionary<string, ParkPricingEntity> pricingByParkId = this.parkPricingRepository is null
            ? new Dictionary<string, ParkPricingEntity>(StringComparer.Ordinal)
            : (await this.parkPricingRepository.GetPublicTextByParkIdsAsync(parkIds, cancellationToken))
                .Where(static pricing => !string.IsNullOrWhiteSpace(pricing.ParkId))
                .GroupBy(static pricing => pricing.ParkId.Trim(), StringComparer.Ordinal)
                .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
        IReadOnlyDictionary<string, ParkDataCompletenessContext> contextsByParkId = await DataCompletenessContextFactory.BuildParkContextsAsync(
            new List<Park> { park },
            countsByParkId,
            openingHoursByParkId,
            new ParkOpeningHoursAdminStatusResolverAccessor(summary => this.openingHoursStatusResolver.ResolveCoverage(summary).Status),
            this.parkItemRepository,
            this.parkZoneRepository,
            this.imageRepository,
            this.historyEventRepository,
            cancellationToken,
            query.ProjectForPublication,
            openingHoursSchedulesByParkId,
            pricingByParkId,
            this.parkFounderRepository,
            this.parkOperatorRepository,
            this.attractionManufacturerRepository);

        ParkDataCompletenessContext? context = !string.IsNullOrWhiteSpace(park.Id) && contextsByParkId.TryGetValue(park.Id, out ParkDataCompletenessContext? resolvedContext)
            ? resolvedContext
            : null;

        return ApplicationResult<DataCompletenessScore>.Success(park.CalculateDataCompletenessScore(context));
    }
}
