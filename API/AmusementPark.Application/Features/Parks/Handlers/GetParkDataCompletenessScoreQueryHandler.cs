using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Services;
using AmusementPark.Core.Domain.Parks;

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

    public GetParkDataCompletenessScoreQueryHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkOpeningHoursRepository parkOpeningHoursRepository,
        ParkOpeningHoursAdminStatusResolver openingHoursStatusResolver,
        IParkZoneRepository? parkZoneRepository = null,
        IImageRepository? imageRepository = null,
        IHistoryEventRepository? historyEventRepository = null)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkOpeningHoursRepository = parkOpeningHoursRepository;
        this.openingHoursStatusResolver = openingHoursStatusResolver;
        this.parkZoneRepository = parkZoneRepository;
        this.imageRepository = imageRepository;
        this.historyEventRepository = historyEventRepository;
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
        IReadOnlyDictionary<string, ParkDataCompletenessContext> contextsByParkId = await DataCompletenessContextFactory.BuildParkContextsAsync(
            new List<Park> { park },
            countsByParkId,
            openingHoursByParkId,
            new ParkOpeningHoursAdminStatusResolverAccessor(summary => this.openingHoursStatusResolver.ResolveCoverage(summary).Status),
            this.parkItemRepository,
            this.parkZoneRepository,
            this.imageRepository,
            this.historyEventRepository,
            cancellationToken);

        ParkDataCompletenessContext? context = !string.IsNullOrWhiteSpace(park.Id) && contextsByParkId.TryGetValue(park.Id, out ParkDataCompletenessContext? resolvedContext)
            ? resolvedContext
            : null;

        return ApplicationResult<DataCompletenessScore>.Success(park.CalculateDataCompletenessScore(context));
    }
}
