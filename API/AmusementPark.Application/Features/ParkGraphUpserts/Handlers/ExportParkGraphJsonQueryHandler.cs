using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
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
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;

public sealed class ExportParkGraphJsonQueryHandler : IQueryHandler<ExportParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>
{
    private static readonly JsonSerializerOptions ExportJsonOptions = BuildExportJsonOptions();
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

    public ExportParkGraphJsonQueryHandler(
        IParkRepository parkRepository,
        IParkZoneRepository parkZoneRepository,
        IParkItemRepository parkItemRepository,
        IParkFounderRepository parkFounderRepository,
        IParkOperatorRepository parkOperatorRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        IImageRepository imageRepository,
        IParkOpeningHoursRepository? openingHoursRepository = null)
    {
        this.parkRepository = parkRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.imageRepository = imageRepository;
        this.openingHoursRepository = openingHoursRepository;
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

        IReadOnlyCollection<ParkZone> zones = await this.parkZoneRepository.GetByParkIdAsync(park.Id, cancellationToken);
        IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByParkIdAsync(park.Id, true, cancellationToken);
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

        IReadOnlyCollection<Image> parkImages = await this.imageRepository.GetByOwnersAsync(ImageOwnerType.Park, new[] { park.Id }, null, cancellationToken);
        IReadOnlyCollection<Image> itemImages = itemIds.Count == 0
            ? Array.Empty<Image>()
            : await this.imageRepository.GetByOwnersAsync(ImageOwnerType.ParkItem, itemIds, null, cancellationToken);
        IReadOnlyCollection<Image> founderImages = founderIds.Count == 0
            ? Array.Empty<Image>()
            : await this.imageRepository.GetByOwnersAsync(ImageOwnerType.ParkFounder, founderIds, null, cancellationToken);
        IReadOnlyCollection<Image> operatorImages = operatorIds.Count == 0
            ? Array.Empty<Image>()
            : await this.imageRepository.GetByOwnersAsync(ImageOwnerType.ParkOperator, operatorIds, null, cancellationToken);
        IReadOnlyCollection<Image> manufacturerImages = manufacturerIds.Count == 0
            ? Array.Empty<Image>()
            : await this.imageRepository.GetByOwnersAsync(ImageOwnerType.AttractionManufacturer, manufacturerIds, null, cancellationToken);
        ParkOpeningHoursSchedule? openingHours = this.openingHoursRepository is null
            ? null
            : await this.openingHoursRepository.GetByParkIdAsync(park.Id, cancellationToken);

        DateTime exportedAtUtc = DateTime.UtcNow;
        ParkGraphExportDocument document = new ParkGraphExportDocument
        {
            Identity = MapIdentity(park),
            References = await this.MapReferencesAsync(park, items, cancellationToken),
            Park = MapPark(park),
            Zones = zones
                .OrderBy(static zone => zone.SortOrder)
                .ThenBy(static zone => zone.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static zone => MapZone(zone))
                .ToList(),
            Items = items
                .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static item => MapItem(item))
                .ToList(),
            Images = parkImages
                .Concat(itemImages)
                .Concat(operatorImages)
                .Concat(founderImages)
                .Concat(manufacturerImages)
                .OrderBy(static image => image.OwnerType.ToString(), StringComparer.Ordinal)
                .ThenBy(static image => image.OwnerId, StringComparer.Ordinal)
                .ThenBy(static image => image.OriginalFileName, StringComparer.OrdinalIgnoreCase)
                .Select(image => MapImage(image, park.Id))
                .ToList(),
            OpeningHours = openingHours is null ? null : MapOpeningHours(openingHours),
            Metadata = new ParkGraphExportMetadata
            {
                ExportedAtUtc = exportedAtUtc,
            },
        };

        string json = JsonSerializer.Serialize(document, ExportJsonOptions);
        return ApplicationResult<ParkGraphJsonExportResult>.Success(new ParkGraphJsonExportResult
        {
            FileName = BuildFileName(park, exportedAtUtc),
            Json = json,
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

    private async Task<ParkGraphExportReferences> MapReferencesAsync(Park park, IReadOnlyCollection<ParkItem> items, CancellationToken cancellationToken)
    {
        List<ParkGraphExportFounder> founders = new List<ParkGraphExportFounder>();
        if (!string.IsNullOrWhiteSpace(park.FounderId))
        {
            ParkFounder? founder = await this.parkFounderRepository.GetByIdAsync(park.FounderId, cancellationToken);
            if (founder is not null)
            {
                founders.Add(MapFounder(founder));
            }
        }

        List<ParkGraphExportOperator> operators = new List<ParkGraphExportOperator>();
        if (!string.IsNullOrWhiteSpace(park.OperatorId))
        {
            ParkOperator? parkOperator = await this.parkOperatorRepository.GetByIdAsync(park.OperatorId, cancellationToken);
            if (parkOperator is not null)
            {
                operators.Add(MapOperator(parkOperator));
            }
        }

        List<string> manufacturerIds = items
            .Select(static item => item.AttractionDetails?.ManufacturerId)
            .Where(static manufacturerId => !string.IsNullOrWhiteSpace(manufacturerId))
            .Select(static manufacturerId => manufacturerId ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static manufacturerId => manufacturerId, StringComparer.Ordinal)
            .ToList();

        List<ParkGraphExportManufacturer> manufacturers = new List<ParkGraphExportManufacturer>();
        foreach (string manufacturerId in manufacturerIds)
        {
            AttractionManufacturer? manufacturer = await this.attractionManufacturerRepository.GetByIdAsync(manufacturerId, cancellationToken);
            if (manufacturer is not null)
            {
                manufacturers.Add(MapManufacturer(manufacturer));
            }
        }

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

    private static ParkGraphExportPark MapPark(Park park)
    {
        return new ParkGraphExportPark
        {
            Id = park.Id,
            Name = park.Name,
            CountryCode = park.CountryCode,
            Type = park.Type,
            Status = park.Status,
            FounderId = park.FounderId,
            FounderKey = park.FounderId,
            OperatorId = park.OperatorId,
            OperatorKey = park.OperatorId,
            Descriptions = CopyLocalizedTexts(park.Descriptions),
            IsVisible = park.IsVisible,
            AdminReviewStatus = park.AdminReviewStatus,
            IsFeaturedOnHome = park.IsFeaturedOnHome,
            FeaturedHomeOrder = park.FeaturedHomeOrder,
            IsFeaturedOnHomeSponsored = park.IsFeaturedOnHomeSponsored,
            WebsiteUrl = park.WebsiteUrl,
            Street = park.Street,
            City = park.City,
            PostalCode = park.PostalCode,
            Latitude = park.Position?.Latitude,
            Longitude = park.Position?.Longitude,
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

    private static ParkGraphExportOpeningHours MapOpeningHours(ParkOpeningHoursSchedule schedule)
    {
        return new ParkGraphExportOpeningHours
        {
            ParkId = schedule.ParkId,
            TimeZoneId = schedule.TimeZoneId,
            SourceUrl = schedule.SourceUrl,
            Notes = schedule.Notes,
            LastVerifiedAtUtc = schedule.LastVerifiedAtUtc,
            RegularRules = schedule.RegularRules
                .OrderBy(static rule => rule.SortOrder)
                .ThenBy(static rule => rule.StartDate)
                .Select(static rule => new ParkGraphExportOpeningHoursRule
                {
                    Id = rule.Id,
                    StartDate = FormatOpeningHoursDate(rule.StartDate),
                    EndDate = FormatOpeningHoursDate(rule.EndDate),
                    DaysOfWeek = rule.DaysOfWeek.Select(static day => day.ToString()).ToList(),
                    IsClosed = rule.IsClosed,
                    Label = rule.Label,
                    Reason = rule.Reason,
                    SortOrder = rule.SortOrder,
                    TimeRanges = rule.TimeRanges.Select(static timeRange => MapOpeningHoursTimeRange(timeRange)).ToList(),
                })
                .ToList(),
            DateOverrides = schedule.DateOverrides
                .OrderBy(static dateOverride => dateOverride.LocalDate)
                .Select(static dateOverride => new ParkGraphExportOpeningHoursDateOverride
                {
                    LocalDate = FormatOpeningHoursDate(dateOverride.LocalDate),
                    IsClosed = dateOverride.IsClosed,
                    Label = dateOverride.Label,
                    Reason = dateOverride.Reason,
                    TimeRanges = dateOverride.TimeRanges.Select(static timeRange => MapOpeningHoursTimeRange(timeRange)).ToList(),
                })
                .ToList(),
        };
    }

    private static ParkGraphExportOpeningHoursTimeRange MapOpeningHoursTimeRange(ParkOpeningHoursTimeRange timeRange)
    {
        return new ParkGraphExportOpeningHoursTimeRange
        {
            OpensAt = FormatOpeningHoursTime(timeRange.OpensAt),
            ClosesAt = FormatOpeningHoursTime(timeRange.ClosesAt),
            ClosesNextDay = timeRange.ClosesNextDay,
            LastAdmissionAt = timeRange.LastAdmissionAt.HasValue ? FormatOpeningHoursTime(timeRange.LastAdmissionAt.Value) : null,
            LastAdmissionNextDay = timeRange.LastAdmissionNextDay,
        };
    }

    private static string? BuildImageOwnerKey(Image image, string parkId)
    {
        if (image.OwnerType == ImageOwnerType.Park && string.Equals(image.OwnerId, parkId, StringComparison.Ordinal))
        {
            return "park";
        }

        if (image.OwnerType == ImageOwnerType.ParkItem)
        {
            return image.OwnerId;
        }

        if (image.OwnerType == ImageOwnerType.ParkOperator)
        {
            return string.IsNullOrWhiteSpace(image.OwnerId) ? null : $"operator:{image.OwnerId}";
        }

        if (image.OwnerType == ImageOwnerType.ParkFounder)
        {
            return string.IsNullOrWhiteSpace(image.OwnerId) ? null : $"founder:{image.OwnerId}";
        }

        if (image.OwnerType == ImageOwnerType.AttractionManufacturer)
        {
            return string.IsNullOrWhiteSpace(image.OwnerId) ? null : $"manufacturer:{image.OwnerId}";
        }

        return image.OwnerId;
    }

    private static string BuildInternalImageUrl(string imageId)
    {
        return $"/images/{imageId}";
    }

    private static List<string> BuildDistinctIds(IEnumerable<string?> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToList();
    }

    private static ParkGraphExportFounder MapFounder(ParkFounder founder)
    {
        return new ParkGraphExportFounder
        {
            Key = founder.Id,
            Id = founder.Id,
            Name = founder.Name,
            Occupation = founder.Occupation,
            BirthDate = founder.BirthDate,
            DeathDate = founder.DeathDate,
            BirthPlace = founder.BirthPlace,
            NationalityCountryCode = founder.NationalityCountryCode,
            WebsiteUrl = founder.WebsiteUrl,
            Biography = CopyLocalizedTexts(founder.Biography),
        };
    }

    private static ParkGraphExportOperator MapOperator(ParkOperator parkOperator)
    {
        return new ParkGraphExportOperator
        {
            Key = parkOperator.Id,
            Id = parkOperator.Id,
            Name = parkOperator.Name,
            LegalName = parkOperator.LegalName,
            FoundedYear = parkOperator.FoundedYear,
            ClosedYear = parkOperator.ClosedYear,
            ContactDetails = parkOperator.ContactDetails,
            Description = CopyLocalizedTexts(parkOperator.Description),
            AdminReviewStatus = parkOperator.AdminReviewStatus,
        };
    }

    private static ParkGraphExportManufacturer MapManufacturer(AttractionManufacturer manufacturer)
    {
        return new ParkGraphExportManufacturer
        {
            Key = manufacturer.Id,
            Id = manufacturer.Id,
            Name = manufacturer.Name,
            LegalName = manufacturer.LegalName,
            FoundedYear = manufacturer.FoundedYear,
            ClosedYear = manufacturer.ClosedYear,
            ContactDetails = manufacturer.ContactDetails,
            Biography = CopyLocalizedTexts(manufacturer.Biography),
            IsVisible = manufacturer.IsVisible,
            AdminReviewStatus = manufacturer.AdminReviewStatus,
        };
    }

    private static List<LocalizedText> CopyLocalizedTexts(IReadOnlyCollection<LocalizedText> values)
    {
        return values
            .Select(static value => new LocalizedText(value.LanguageCode, value.Value))
            .ToList();
    }

    private static string BuildFileName(Park park, DateTime exportedAtUtc)
    {
        string sourceName = string.IsNullOrWhiteSpace(park.Name) ? park.Id : park.Name;
        string safeName = SanitizeFileName(sourceName);
        return $"{safeName}-{exportedAtUtc.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-park-graph.json";
    }

    private static string SanitizeFileName(string value)
    {
        StringBuilder builder = new StringBuilder();
        foreach (char character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (character == '-' || character == '_')
            {
                builder.Append(character);
            }
            else if (char.IsWhiteSpace(character))
            {
                builder.Append('-');
            }
        }

        string result = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(result) ? "park" : result;
    }

    private static string FormatOpeningHoursDate(DateOnly date)
    {
        return date.ToString(OpeningHoursDateFormat, CultureInfo.InvariantCulture);
    }

    private static string FormatOpeningHoursTime(TimeOnly time)
    {
        return time.ToString(OpeningHoursTimeFormat, CultureInfo.InvariantCulture);
    }
}
