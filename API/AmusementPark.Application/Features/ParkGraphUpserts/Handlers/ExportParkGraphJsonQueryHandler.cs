using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;

public sealed partial class ExportParkGraphJsonQueryHandler : IQueryHandler<ExportParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>
{
    private static readonly JsonSerializerOptions ExportJsonOptions = BuildExportJsonOptions();
    private static readonly IReadOnlySet<ParkGraphExportSection> AllExportSections = Enum.GetValues<ParkGraphExportSection>().ToHashSet();
    private const string OpeningHoursDateFormat = "yyyy-MM-dd";
    private const string OpeningHoursTimeFormat = "HH:mm";

    private readonly IParkRepository parkRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkFounderRepository parkFounderRepository;
    private readonly IParkOperatorRepository parkOperatorRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly IImageRepository imageRepository;
    private readonly IParkOpeningHoursRepository? openingHoursRepository;
    private readonly IHistoryEventRepository? historyEventRepository;

    public ExportParkGraphJsonQueryHandler(
        IParkRepository parkRepository,
        IParkZoneRepository parkZoneRepository,
        IParkItemRepository parkItemRepository,
        IParkFounderRepository parkFounderRepository,
        IParkOperatorRepository parkOperatorRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        IImageRepository imageRepository,
        IParkOpeningHoursRepository? openingHoursRepository = null,
        IHistoryEventRepository? historyEventRepository = null)
    {
        this.parkRepository = parkRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.imageRepository = imageRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.historyEventRepository = historyEventRepository;
    }

    public async Task<ApplicationResult<ParkGraphJsonExportResult>> HandleAsync(ExportParkGraphJsonQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<ParkGraphJsonExportResult>.Failure(ApplicationErrors.Required("parkId"));
        }

        Park? park = await this.parkRepository.GetByIdAsync(query.ParkId.Trim(), true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkGraphJsonExportResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Park), query.ParkId));
        }

        return await this.ExportResolvedParkAsync(park, query.Sections, cancellationToken);
    }

    private async Task<ApplicationResult<ParkGraphJsonExportResult>> ExportResolvedParkAsync(
        Park park,
        IReadOnlyCollection<ParkGraphExportSection>? requestedSections,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(park);

        IReadOnlySet<ParkGraphExportSection> sections = ResolveSections(requestedSections);
        bool includeZones = sections.Contains(ParkGraphExportSection.Zones);
        bool includeItems = sections.Contains(ParkGraphExportSection.Items);
        bool includeReferences = sections.Contains(ParkGraphExportSection.References);
        bool includeImages = sections.Contains(ParkGraphExportSection.Images);
        bool includeOpeningHours = sections.Contains(ParkGraphExportSection.OpeningHours);
        bool includeHistory = sections.Contains(ParkGraphExportSection.History);
        bool needsItems = includeItems || includeReferences || includeHistory;

        Task<IReadOnlyCollection<ParkZone>> zonesTask = includeZones
            ? this.parkZoneRepository.GetByParkIdAsync(park.Id, cancellationToken)
            : Task.FromResult<IReadOnlyCollection<ParkZone>>(Array.Empty<ParkZone>());
        Task<IReadOnlyCollection<ParkItem>> itemsTask = needsItems
            ? this.parkItemRepository.GetByParkIdAsync(park.Id, true, cancellationToken)
            : Task.FromResult<IReadOnlyCollection<ParkItem>>(Array.Empty<ParkItem>());
        await Task.WhenAll(zonesTask, itemsTask);

        IReadOnlyCollection<ParkZone> zones = await zonesTask;
        IReadOnlyCollection<ParkItem> items = await itemsTask;
        List<string> itemIds = items
            .Select(static item => item.Id)
            .Where(static itemId => !string.IsNullOrWhiteSpace(itemId))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        List<string> founderIds = BuildDistinctIds(new[] { park.FounderId });
        List<string> operatorIds = BuildDistinctIds(new[] { park.OperatorId });
        List<string> manufacturerIds = items
            .Select(static item => item.AttractionDetails?.ManufacturerId)
            .Where(static manufacturerId => !string.IsNullOrWhiteSpace(manufacturerId))
            .Select(static manufacturerId => manufacturerId ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static manufacturerId => manufacturerId, StringComparer.Ordinal)
            .ToList();

        Task<IReadOnlyCollection<Image>> parkImagesTask = includeImages
            ? this.imageRepository.GetByOwnersAsync(ImageOwnerType.Park, new[] { park.Id }, null, cancellationToken)
            : Task.FromResult<IReadOnlyCollection<Image>>(Array.Empty<Image>());
        Task<IReadOnlyCollection<Image>> itemImagesTask = includeImages && includeItems && itemIds.Count > 0
            ? this.imageRepository.GetByOwnersAsync(ImageOwnerType.ParkItem, itemIds, null, cancellationToken)
            : Task.FromResult<IReadOnlyCollection<Image>>(Array.Empty<Image>());
        Task<IReadOnlyCollection<Image>> founderImagesTask = includeImages && includeReferences && founderIds.Count > 0
            ? this.imageRepository.GetByOwnersAsync(ImageOwnerType.ParkFounder, founderIds, null, cancellationToken)
            : Task.FromResult<IReadOnlyCollection<Image>>(Array.Empty<Image>());
        Task<IReadOnlyCollection<Image>> operatorImagesTask = includeImages && includeReferences && operatorIds.Count > 0
            ? this.imageRepository.GetByOwnersAsync(ImageOwnerType.ParkOperator, operatorIds, null, cancellationToken)
            : Task.FromResult<IReadOnlyCollection<Image>>(Array.Empty<Image>());
        Task<IReadOnlyCollection<Image>> manufacturerImagesTask = includeImages && includeReferences && manufacturerIds.Count > 0
            ? this.imageRepository.GetByOwnersAsync(ImageOwnerType.AttractionManufacturer, manufacturerIds, null, cancellationToken)
            : Task.FromResult<IReadOnlyCollection<Image>>(Array.Empty<Image>());
        Task<ParkOpeningHoursSchedule?> openingHoursTask = !includeOpeningHours || this.openingHoursRepository is null
            ? Task.FromResult<ParkOpeningHoursSchedule?>(null)
            : this.openingHoursRepository.GetByParkIdAsync(park.Id, cancellationToken);
        Task<IReadOnlyCollection<HistoryEvent>> historyEventsTask = !includeHistory || this.historyEventRepository is null
            ? Task.FromResult<IReadOnlyCollection<HistoryEvent>>(Array.Empty<HistoryEvent>())
            : this.historyEventRepository.GetParkTimelineAsync(park.Id, true, true, itemIds, cancellationToken);
        Task<ParkGraphExportReferences?> referencesTask = includeReferences
            ? this.MapOptionalReferencesAsync(park, manufacturerIds, cancellationToken)
            : Task.FromResult<ParkGraphExportReferences?>(null);

        await Task.WhenAll(
            parkImagesTask,
            itemImagesTask,
            founderImagesTask,
            operatorImagesTask,
            manufacturerImagesTask,
            openingHoursTask,
            historyEventsTask,
            referencesTask);

        IReadOnlyCollection<Image> parkImages = await parkImagesTask;
        IReadOnlyCollection<Image> itemImages = await itemImagesTask;
        IReadOnlyCollection<Image> founderImages = await founderImagesTask;
        IReadOnlyCollection<Image> operatorImages = await operatorImagesTask;
        IReadOnlyCollection<Image> manufacturerImages = await manufacturerImagesTask;
        ParkOpeningHoursSchedule? openingHours = await openingHoursTask;
        IReadOnlyCollection<HistoryEvent> historyEvents = await historyEventsTask;
        ParkGraphExportReferences? references = await referencesTask;

        DateTime exportedAtUtc = DateTime.UtcNow;
        Dictionary<string, object?> document = new Dictionary<string, object?>
        {
            ["documentType"] = "AmusementParkParkGraphUpsert",
            ["schemaVersion"] = "2026-06-30",
            ["mode"] = "merge",
            ["identity"] = MapIdentity(park),
        };

        Dictionary<string, object?> parkPatch = MapParkPatch(park, sections);
        if (parkPatch.Count > 0)
        {
            document["park"] = parkPatch;
        }

        if (includeReferences)
        {
            document["references"] = references ?? new ParkGraphExportReferences();
        }

        if (includeZones)
        {
            document["zones"] = zones
                .OrderBy(static zone => zone.SortOrder)
                .ThenBy(static zone => zone.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static zone => MapZone(zone))
                .ToList();
        }

        if (includeItems)
        {
            document["items"] = items
                .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static item => MapItem(item))
                .ToList();
        }

        if (includeImages)
        {
            document["images"] = parkImages
                .Concat(itemImages)
                .Concat(operatorImages)
                .Concat(founderImages)
                .Concat(manufacturerImages)
                .OrderBy(static image => image.OwnerType.ToString(), StringComparer.Ordinal)
                .ThenBy(static image => image.OwnerId, StringComparer.Ordinal)
                .ThenBy(static image => image.OriginalFileName, StringComparer.OrdinalIgnoreCase)
                .Select(image => MapImage(image, park.Id))
                .ToList();
        }

        if (includeOpeningHours)
        {
            document["openingHours"] = openingHours is null ? null : MapOpeningHours(openingHours);
        }

        if (includeHistory)
        {
            document["history"] = MapHistory(historyEvents);
        }

        document["metadata"] = new ParkGraphExportMetadata
        {
            ExportedAtUtc = exportedAtUtc,
        };

        byte[] content = JsonSerializer.SerializeToUtf8Bytes(document, ExportJsonOptions);
        return ApplicationResult<ParkGraphJsonExportResult>.Success(new ParkGraphJsonExportResult
        {
            FileName = BuildFileName(park, exportedAtUtc),
            Content = content,
        });
    }

    private static JsonSerializerOptions BuildExportJsonOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static IReadOnlySet<ParkGraphExportSection> ResolveSections(IReadOnlyCollection<ParkGraphExportSection>? sections)
    {
        if (sections is null)
        {
            return AllExportSections;
        }

        return sections
            .Distinct()
            .ToHashSet();
    }

    private async Task<ParkGraphExportReferences?> MapOptionalReferencesAsync(Park park, IReadOnlyCollection<string> manufacturerIds, CancellationToken cancellationToken)
    {
        return await this.MapReferencesAsync(park, manufacturerIds, cancellationToken);
    }

    private static Dictionary<string, object?> MapParkPatch(Park park, IReadOnlySet<ParkGraphExportSection> sections)
    {
        Dictionary<string, object?> patch = new Dictionary<string, object?>();

        if (sections.Contains(ParkGraphExportSection.ParkBasics))
        {
            patch["id"] = park.Id;
            patch["name"] = park.Name;
            patch["countryCode"] = park.CountryCode;
            patch["type"] = park.Type;
            patch["status"] = park.Status;
            patch["openingDate"] = park.OpeningDate;
            patch["closingDate"] = park.ClosingDate;
            patch["openingDateText"] = park.OpeningDateText;
            patch["closingDateText"] = park.ClosingDateText;
            patch["founderId"] = park.FounderId;
            patch["founderKey"] = park.FounderId;
            patch["operatorId"] = park.OperatorId;
            patch["operatorKey"] = park.OperatorId;
            patch["websiteUrl"] = park.WebsiteUrl;
        }

        if (sections.Contains(ParkGraphExportSection.ParkAudience))
        {
            patch["audienceClassification"] = park.AudienceClassification;
        }

        if (sections.Contains(ParkGraphExportSection.ParkLocation))
        {
            patch["countryCode"] = park.CountryCode;
            patch["street"] = park.Street;
            patch["city"] = park.City;
            patch["postalCode"] = park.PostalCode;
            patch["latitude"] = park.Position?.Latitude;
            patch["longitude"] = park.Position?.Longitude;
        }

        if (sections.Contains(ParkGraphExportSection.ParkAdministration))
        {
            patch["isVisible"] = park.IsVisible;
            patch["adminReviewStatus"] = park.AdminReviewStatus;
        }

        if (sections.Contains(ParkGraphExportSection.ParkDescriptions))
        {
            patch["descriptions"] = CopyLocalizedTexts(park.Descriptions);
        }

        if (sections.Contains(ParkGraphExportSection.ParkHomeFeature))
        {
            patch["isFeaturedOnHome"] = park.IsFeaturedOnHome;
            patch["featuredHomeOrder"] = park.FeaturedHomeOrder;
            patch["isFeaturedOnHomeSponsored"] = park.IsFeaturedOnHomeSponsored;
        }

        return patch;
    }

    private async Task<ParkGraphExportReferences> MapReferencesAsync(Park park, IReadOnlyCollection<string> manufacturerIds, CancellationToken cancellationToken)
    {
        Task<ParkFounder?> founderTask = string.IsNullOrWhiteSpace(park.FounderId)
            ? Task.FromResult<ParkFounder?>(null)
            : this.parkFounderRepository.GetByIdAsync(park.FounderId, cancellationToken);
        Task<ParkOperator?> operatorTask = string.IsNullOrWhiteSpace(park.OperatorId)
            ? Task.FromResult<ParkOperator?>(null)
            : this.parkOperatorRepository.GetByIdAsync(park.OperatorId, cancellationToken);
        Task<IReadOnlyCollection<AttractionManufacturer>> manufacturersTask = manufacturerIds.Count == 0
            ? Task.FromResult<IReadOnlyCollection<AttractionManufacturer>>(Array.Empty<AttractionManufacturer>())
            : this.attractionManufacturerRepository.GetByIdsAsync(manufacturerIds, cancellationToken);

        await Task.WhenAll(founderTask, operatorTask, manufacturersTask);

        ParkFounder? founder = await founderTask;
        ParkOperator? parkOperator = await operatorTask;
        IReadOnlyCollection<AttractionManufacturer> manufacturerEntities = await manufacturersTask;

        List<ParkGraphExportFounder> founders = founder is null
            ? new List<ParkGraphExportFounder>()
            : new List<ParkGraphExportFounder> { MapFounder(founder) };
        List<ParkGraphExportOperator> operators = parkOperator is null
            ? new List<ParkGraphExportOperator>()
            : new List<ParkGraphExportOperator> { MapOperator(parkOperator) };
        List<ParkGraphExportManufacturer> manufacturers = manufacturerEntities
            .OrderBy(static manufacturer => manufacturer.Id, StringComparer.Ordinal)
            .Select(static manufacturer => MapManufacturer(manufacturer))
            .ToList();

        return new ParkGraphExportReferences
        {
            Founders = founders,
            Operators = operators,
            Manufacturers = manufacturers,
        };
    }

    private static ParkGraphExportIdentity MapIdentity(Park park)
    {
        return new ParkGraphExportIdentity
        {
            ParkId = park.Id,
            Id = park.Id,
            Name = park.Name,
            CountryCode = park.CountryCode,
        };
    }

    private static ParkGraphExportZone MapZone(ParkZone zone)
    {
        return new ParkGraphExportZone
        {
            Key = zone.Id,
            Id = zone.Id,
            Name = zone.Name,
            Names = CopyLocalizedTexts(zone.Names),
            Slug = zone.Slug,
            Descriptions = CopyLocalizedTexts(zone.Descriptions),
            IsVisible = zone.IsVisible,
            SortOrder = zone.SortOrder,
            Latitude = zone.Position?.Latitude,
            Longitude = zone.Position?.Longitude,
        };
    }

    private static ParkGraphExportItem MapItem(ParkItem item)
    {
        return new ParkGraphExportItem
        {
            Key = item.Id,
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            Type = item.Type,
            Subtype = item.Subtype,
            ZoneId = item.ZoneId,
            ZoneKey = item.ZoneId,
            Descriptions = CopyLocalizedTexts(item.Descriptions),
            AttractionDetails = item.AttractionDetails is null ? null : MapAttractionDetails(item.AttractionDetails),
            AttractionLocations = item.AttractionLocations,
            IsVisible = item.IsVisible,
            AdminReviewStatus = item.AdminReviewStatus,
            Latitude = item.Position?.Latitude,
            Longitude = item.Position?.Longitude,
        };
    }

    private static ParkGraphExportAttractionDetails MapAttractionDetails(AttractionDetails details)
    {
        return new ParkGraphExportAttractionDetails
        {
            ManufacturerId = details.ManufacturerId,
            ManufacturerKey = details.ManufacturerId,
            Model = details.Model,
            ExternalSource = details.ExternalSource,
            ExternalId = details.ExternalId,
            SourceUrl = details.SourceUrl,
            Status = details.Status,
            MaterialType = details.MaterialType,
            SeatingType = details.SeatingType,
            LaunchType = details.LaunchType,
            RestraintType = details.RestraintType,
            IsLaunched = details.IsLaunched,
            OpeningDate = details.OpeningDate,
            ClosingDate = details.ClosingDate,
            OpeningDateText = details.OpeningDateText,
            ClosingDateText = details.ClosingDateText,
            DurationInSeconds = details.DurationInSeconds,
            CapacityPerHour = details.CapacityPerHour,
            HeightInFeet = details.HeightInFeet,
            HeightInMeters = details.HeightInMeters,
            LengthInFeet = details.LengthInFeet,
            LengthInMeters = details.LengthInMeters,
            SpeedInMph = details.SpeedInMph,
            SpeedInKmH = details.SpeedInKmH,
            DropInFeet = details.DropInFeet,
            DropInMeters = details.DropInMeters,
            InversionCount = details.InversionCount,
            TrainCount = details.TrainCount,
            CarsPerTrain = details.CarsPerTrain,
            RidersPerVehicle = details.RidersPerVehicle,
            HasSingleRider = details.HasSingleRider,
            HasFastPass = details.HasFastPass,
            IsAccessibleForReducedMobility = details.IsAccessibleForReducedMobility,
            IsIndoor = details.IsIndoor,
            WaterExposureLevel = details.WaterExposureLevel,
            AccessConditions = details.AccessConditions.ToList(),
        };
    }

    private static ParkGraphExportImage MapImage(Image image, string parkId)
    {
        return new ParkGraphExportImage
        {
            ImageId = image.Id,
            Id = image.Id,
            OwnerType = image.OwnerType,
            OwnerId = image.OwnerId,
            OwnerKey = BuildImageOwnerKey(image, parkId),
            Category = image.Category,
            IsPublished = image.IsPublished,
            IsCurrent = image.IsCurrent,
            SetAsCurrent = image.IsCurrent,
            WithWatermark = false,
            IsWatermarked = image.IsWatermarked,
            SourceUrl = image.SourceUrl,
            InternalUrl = BuildInternalImageUrl(image.Id),
            Description = image.Description,
            AltTexts = CopyLocalizedTexts(image.AltTexts),
            Captions = CopyLocalizedTexts(image.Captions),
            Credits = CopyLocalizedTexts(image.Credits),
            TagIds = image.TagIds.ToList(),
            GeoLocation = image.GeoLocation,
            OriginalFileName = image.OriginalFileName,
            ContentType = image.ContentType,
            Width = image.Width,
            Height = image.Height,
            SizeInBytes = image.SizeInBytes,
        };
    }

}
