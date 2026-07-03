using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed class BulkParkGraphJsonExportDataLoader
{
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkFounderRepository parkFounderRepository;
    private readonly IParkOperatorRepository parkOperatorRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly IImageRepository imageRepository;
    private readonly IParkOpeningHoursRepository? openingHoursRepository;
    private readonly IHistoryEventRepository? historyEventRepository;

    public BulkParkGraphJsonExportDataLoader(
        IParkZoneRepository parkZoneRepository,
        IParkItemRepository parkItemRepository,
        IParkFounderRepository parkFounderRepository,
        IParkOperatorRepository parkOperatorRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        IImageRepository imageRepository,
        IParkOpeningHoursRepository? openingHoursRepository = null,
        IHistoryEventRepository? historyEventRepository = null)
    {
        this.parkZoneRepository = parkZoneRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.imageRepository = imageRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.historyEventRepository = historyEventRepository;
    }

    public async Task<BulkParkGraphJsonExportData> LoadAsync(
        IReadOnlyCollection<Park> parks,
        IReadOnlySet<ParkGraphExportSection> sections,
        CancellationToken cancellationToken)
    {
        List<string> parkIds = ParkGraphJsonExportDocumentFactory.BuildDistinctIds(parks.Select(static park => park.Id));
        bool includeZones = sections.Contains(ParkGraphExportSection.Zones);
        bool includeItems = sections.Contains(ParkGraphExportSection.Items);
        bool includeReferences = sections.Contains(ParkGraphExportSection.References);
        bool includeImages = sections.Contains(ParkGraphExportSection.Images);
        bool includeOpeningHours = sections.Contains(ParkGraphExportSection.OpeningHours);
        bool includeHistory = sections.Contains(ParkGraphExportSection.History);
        bool needsItems = includeItems || includeReferences || includeHistory;

        IReadOnlyCollection<ParkItem> items = needsItems
            ? await this.parkItemRepository.GetByParkIdsAsync(parkIds, true, cancellationToken)
            : Array.Empty<ParkItem>();
        Dictionary<string, IReadOnlyCollection<ParkItem>> itemsByParkId = GroupByKey(
            items,
            static item => item.ParkId);

        Dictionary<string, IReadOnlyCollection<ParkZone>> zonesByParkId = includeZones
            ? await this.LoadZonesByParkIdAsync(parkIds, cancellationToken)
            : new Dictionary<string, IReadOnlyCollection<ParkZone>>(StringComparer.Ordinal);
        Dictionary<string, ParkOpeningHoursSchedule?> openingHoursByParkId = includeOpeningHours
            ? await this.LoadOpeningHoursByParkIdAsync(parkIds, cancellationToken)
            : new Dictionary<string, ParkOpeningHoursSchedule?>(StringComparer.Ordinal);
        Dictionary<string, IReadOnlyCollection<HistoryEvent>> historyEventsByParkId = includeHistory
            ? await this.LoadHistoryEventsByParkIdAsync(parkIds, itemsByParkId, cancellationToken)
            : new Dictionary<string, IReadOnlyCollection<HistoryEvent>>(StringComparer.Ordinal);
        Dictionary<string, ParkGraphExportReferences?> referencesByParkId = includeReferences
            ? await this.LoadReferencesByParkIdAsync(parks, itemsByParkId, cancellationToken)
            : new Dictionary<string, ParkGraphExportReferences?>(StringComparer.Ordinal);
        Dictionary<string, IReadOnlyCollection<Image>> imagesByParkId = includeImages
            ? await this.LoadImagesByParkIdAsync(parks, items, itemsByParkId, includeItems, includeReferences, cancellationToken)
            : new Dictionary<string, IReadOnlyCollection<Image>>(StringComparer.Ordinal);

        return new BulkParkGraphJsonExportData
        {
            ZonesByParkId = zonesByParkId,
            ItemsByParkId = itemsByParkId,
            ReferencesByParkId = referencesByParkId,
            ImagesByParkId = imagesByParkId,
            OpeningHoursByParkId = openingHoursByParkId,
            HistoryEventsByParkId = historyEventsByParkId,
        };
    }

    private async Task<Dictionary<string, IReadOnlyCollection<ParkZone>>> LoadZonesByParkIdAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        Dictionary<string, IReadOnlyCollection<ParkZone>> zonesByParkId = new Dictionary<string, IReadOnlyCollection<ParkZone>>(StringComparer.Ordinal);
        foreach (string parkId in parkIds)
        {
            zonesByParkId[parkId] = await this.parkZoneRepository.GetByParkIdAsync(parkId, cancellationToken);
        }

        return zonesByParkId;
    }

    private async Task<Dictionary<string, ParkOpeningHoursSchedule?>> LoadOpeningHoursByParkIdAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        Dictionary<string, ParkOpeningHoursSchedule?> openingHoursByParkId = new Dictionary<string, ParkOpeningHoursSchedule?>(StringComparer.Ordinal);
        if (this.openingHoursRepository is null)
        {
            return openingHoursByParkId;
        }

        foreach (string parkId in parkIds)
        {
            openingHoursByParkId[parkId] = await this.openingHoursRepository.GetByParkIdAsync(parkId, cancellationToken);
        }

        return openingHoursByParkId;
    }

    private async Task<Dictionary<string, IReadOnlyCollection<HistoryEvent>>> LoadHistoryEventsByParkIdAsync(
        IReadOnlyCollection<string> parkIds,
        IReadOnlyDictionary<string, IReadOnlyCollection<ParkItem>> itemsByParkId,
        CancellationToken cancellationToken)
    {
        Dictionary<string, IReadOnlyCollection<HistoryEvent>> historyEventsByParkId = new Dictionary<string, IReadOnlyCollection<HistoryEvent>>(StringComparer.Ordinal);
        if (this.historyEventRepository is null)
        {
            return historyEventsByParkId;
        }

        foreach (string parkId in parkIds)
        {
            IReadOnlyCollection<string> itemIds = ResolveCollection(itemsByParkId, parkId)
                .Select(static item => item.Id)
                .Where(static itemId => !string.IsNullOrWhiteSpace(itemId))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            historyEventsByParkId[parkId] = await this.historyEventRepository.GetParkTimelineAsync(
                parkId,
                true,
                true,
                itemIds,
                cancellationToken);
        }

        return historyEventsByParkId;
    }

    private async Task<Dictionary<string, ParkGraphExportReferences?>> LoadReferencesByParkIdAsync(
        IReadOnlyCollection<Park> parks,
        IReadOnlyDictionary<string, IReadOnlyCollection<ParkItem>> itemsByParkId,
        CancellationToken cancellationToken)
    {
        List<string> founderIds = ParkGraphJsonExportDocumentFactory.BuildDistinctIds(parks.Select(static park => park.FounderId));
        List<string> operatorIds = ParkGraphJsonExportDocumentFactory.BuildDistinctIds(parks.Select(static park => park.OperatorId));
        List<string> manufacturerIds = BuildManufacturerIds(itemsByParkId.Values.SelectMany(static items => items));

        IReadOnlyCollection<ParkFounder> founders = founderIds.Count == 0
            ? Array.Empty<ParkFounder>()
            : await this.parkFounderRepository.GetAllAsync(cancellationToken);
        IReadOnlyCollection<ParkOperator> operators = operatorIds.Count == 0
            ? Array.Empty<ParkOperator>()
            : await this.parkOperatorRepository.GetAllAsync(cancellationToken);
        IReadOnlyCollection<AttractionManufacturer> manufacturers = manufacturerIds.Count == 0
            ? Array.Empty<AttractionManufacturer>()
            : await this.attractionManufacturerRepository.GetByIdsAsync(manufacturerIds, cancellationToken);

        Dictionary<string, ParkFounder> foundersById = founders
            .Where(founder => founderIds.Contains(founder.Id))
            .ToDictionary(static founder => founder.Id, StringComparer.Ordinal);
        Dictionary<string, ParkOperator> operatorsById = operators
            .Where(parkOperator => operatorIds.Contains(parkOperator.Id))
            .ToDictionary(static parkOperator => parkOperator.Id, StringComparer.Ordinal);
        Dictionary<string, AttractionManufacturer> manufacturersById = manufacturers
            .ToDictionary(static manufacturer => manufacturer.Id, StringComparer.Ordinal);
        Dictionary<string, ParkGraphExportReferences?> referencesByParkId = new Dictionary<string, ParkGraphExportReferences?>(StringComparer.Ordinal);

        foreach (Park park in parks)
        {
            ParkFounder? founder = !string.IsNullOrWhiteSpace(park.FounderId) && foundersById.TryGetValue(park.FounderId, out ParkFounder? resolvedFounder)
                ? resolvedFounder
                : null;
            ParkOperator? parkOperator = !string.IsNullOrWhiteSpace(park.OperatorId) && operatorsById.TryGetValue(park.OperatorId, out ParkOperator? resolvedOperator)
                ? resolvedOperator
                : null;
            List<AttractionManufacturer> parkManufacturers = BuildManufacturerIds(ResolveCollection(itemsByParkId, park.Id))
                .Where(manufacturersById.ContainsKey)
                .Select(manufacturerId => manufacturersById[manufacturerId])
                .ToList();
            referencesByParkId[park.Id] = ParkGraphJsonExportDocumentFactory.BuildReferences(founder, parkOperator, parkManufacturers);
        }

        return referencesByParkId;
    }

    private async Task<Dictionary<string, IReadOnlyCollection<Image>>> LoadImagesByParkIdAsync(
        IReadOnlyCollection<Park> parks,
        IReadOnlyCollection<ParkItem> items,
        IReadOnlyDictionary<string, IReadOnlyCollection<ParkItem>> itemsByParkId,
        bool includeItems,
        bool includeReferences,
        CancellationToken cancellationToken)
    {
        List<string> parkIds = ParkGraphJsonExportDocumentFactory.BuildDistinctIds(parks.Select(static park => park.Id));
        List<string> itemIds = includeItems
            ? ParkGraphJsonExportDocumentFactory.BuildDistinctIds(items.Select(static item => item.Id))
            : new List<string>();
        List<string> founderIds = includeReferences
            ? ParkGraphJsonExportDocumentFactory.BuildDistinctIds(parks.Select(static park => park.FounderId))
            : new List<string>();
        List<string> operatorIds = includeReferences
            ? ParkGraphJsonExportDocumentFactory.BuildDistinctIds(parks.Select(static park => park.OperatorId))
            : new List<string>();
        List<string> manufacturerIds = includeReferences
            ? BuildManufacturerIds(items)
            : new List<string>();

        Task<IReadOnlyCollection<Image>> parkImagesTask = this.imageRepository.GetByOwnersAsync(ImageOwnerType.Park, parkIds, null, cancellationToken);
        Task<IReadOnlyCollection<Image>> itemImagesTask = itemIds.Count == 0
            ? Task.FromResult<IReadOnlyCollection<Image>>(Array.Empty<Image>())
            : this.imageRepository.GetByOwnersAsync(ImageOwnerType.ParkItem, itemIds, null, cancellationToken);
        Task<IReadOnlyCollection<Image>> founderImagesTask = founderIds.Count == 0
            ? Task.FromResult<IReadOnlyCollection<Image>>(Array.Empty<Image>())
            : this.imageRepository.GetByOwnersAsync(ImageOwnerType.ParkFounder, founderIds, null, cancellationToken);
        Task<IReadOnlyCollection<Image>> operatorImagesTask = operatorIds.Count == 0
            ? Task.FromResult<IReadOnlyCollection<Image>>(Array.Empty<Image>())
            : this.imageRepository.GetByOwnersAsync(ImageOwnerType.ParkOperator, operatorIds, null, cancellationToken);
        Task<IReadOnlyCollection<Image>> manufacturerImagesTask = manufacturerIds.Count == 0
            ? Task.FromResult<IReadOnlyCollection<Image>>(Array.Empty<Image>())
            : this.imageRepository.GetByOwnersAsync(ImageOwnerType.AttractionManufacturer, manufacturerIds, null, cancellationToken);

        await Task.WhenAll(parkImagesTask, itemImagesTask, founderImagesTask, operatorImagesTask, manufacturerImagesTask);

        IReadOnlyDictionary<string, IReadOnlyCollection<Image>> parkImagesByOwner = GroupByKey(await parkImagesTask, static image => image.OwnerId);
        IReadOnlyDictionary<string, IReadOnlyCollection<Image>> itemImagesByOwner = GroupByKey(await itemImagesTask, static image => image.OwnerId);
        IReadOnlyDictionary<string, IReadOnlyCollection<Image>> founderImagesByOwner = GroupByKey(await founderImagesTask, static image => image.OwnerId);
        IReadOnlyDictionary<string, IReadOnlyCollection<Image>> operatorImagesByOwner = GroupByKey(await operatorImagesTask, static image => image.OwnerId);
        IReadOnlyDictionary<string, IReadOnlyCollection<Image>> manufacturerImagesByOwner = GroupByKey(await manufacturerImagesTask, static image => image.OwnerId);
        Dictionary<string, IReadOnlyCollection<Image>> imagesByParkId = new Dictionary<string, IReadOnlyCollection<Image>>(StringComparer.Ordinal);

        foreach (Park park in parks)
        {
            List<Image> parkImages = new List<Image>();
            parkImages.AddRange(ResolveCollection(parkImagesByOwner, park.Id));
            if (includeItems)
            {
                foreach (ParkItem item in ResolveCollection(itemsByParkId, park.Id))
                {
                    parkImages.AddRange(ResolveCollection(itemImagesByOwner, item.Id));
                }
            }

            if (includeReferences && !string.IsNullOrWhiteSpace(park.FounderId))
            {
                parkImages.AddRange(ResolveCollection(founderImagesByOwner, park.FounderId));
            }

            if (includeReferences && !string.IsNullOrWhiteSpace(park.OperatorId))
            {
                parkImages.AddRange(ResolveCollection(operatorImagesByOwner, park.OperatorId));
            }

            foreach (string manufacturerId in includeReferences ? BuildManufacturerIds(ResolveCollection(itemsByParkId, park.Id)) : new List<string>())
            {
                parkImages.AddRange(ResolveCollection(manufacturerImagesByOwner, manufacturerId));
            }

            imagesByParkId[park.Id] = parkImages;
        }

        return imagesByParkId;
    }

    private static List<string> BuildManufacturerIds(IEnumerable<ParkItem> items)
    {
        return ParkGraphJsonExportDocumentFactory.BuildDistinctIds(items.Select(static item => item.AttractionDetails?.ManufacturerId));
    }

    private static Dictionary<string, IReadOnlyCollection<TValue>> GroupByKey<TValue>(
        IEnumerable<TValue> values,
        Func<TValue, string?> keySelector)
    {
        return values
            .Select(value => new { Key = keySelector(value), Value = value })
            .Where(static entry => !string.IsNullOrWhiteSpace(entry.Key))
            .GroupBy(static entry => entry.Key ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyCollection<TValue>)group.Select(static entry => entry.Value).ToList(),
                StringComparer.Ordinal);
    }

    private static IReadOnlyCollection<TValue> ResolveCollection<TValue>(
        IReadOnlyDictionary<string, IReadOnlyCollection<TValue>> valuesByParkId,
        string key)
    {
        return valuesByParkId.TryGetValue(key, out IReadOnlyCollection<TValue>? values)
            ? values
            : Array.Empty<TValue>();
    }
}

public sealed class BulkParkGraphJsonExportData
{
    public IReadOnlyDictionary<string, IReadOnlyCollection<ParkZone>> ZonesByParkId { get; init; } =
        new Dictionary<string, IReadOnlyCollection<ParkZone>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, IReadOnlyCollection<ParkItem>> ItemsByParkId { get; init; } =
        new Dictionary<string, IReadOnlyCollection<ParkItem>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ParkGraphExportReferences?> ReferencesByParkId { get; init; } =
        new Dictionary<string, ParkGraphExportReferences?>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, IReadOnlyCollection<Image>> ImagesByParkId { get; init; } =
        new Dictionary<string, IReadOnlyCollection<Image>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ParkOpeningHoursSchedule?> OpeningHoursByParkId { get; init; } =
        new Dictionary<string, ParkOpeningHoursSchedule?>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, IReadOnlyCollection<HistoryEvent>> HistoryEventsByParkId { get; init; } =
        new Dictionary<string, IReadOnlyCollection<HistoryEvent>>(StringComparer.Ordinal);
}
