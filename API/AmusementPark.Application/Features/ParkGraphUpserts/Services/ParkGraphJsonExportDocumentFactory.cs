using System.Globalization;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

internal sealed class ParkGraphJsonParkExportData
{
    public Park Park { get; init; } = new Park();

    public ParkGraphExportReferences? References { get; init; }

    public IReadOnlyCollection<ParkZone> Zones { get; init; } = Array.Empty<ParkZone>();

    public IReadOnlyCollection<ParkItem> Items { get; init; } = Array.Empty<ParkItem>();

    public IReadOnlyCollection<Image> Images { get; init; } = Array.Empty<Image>();

    public ParkOpeningHoursSchedule? OpeningHours { get; init; }

    public IReadOnlyCollection<HistoryEvent> HistoryEvents { get; init; } = Array.Empty<HistoryEvent>();
}

internal static class ParkGraphJsonExportDocumentFactory
{
    private const string OpeningHoursDateFormat = "yyyy-MM-dd";
    private const string OpeningHoursTimeFormat = "HH:mm";

    public static Dictionary<string, object?> BuildDocument(
        ParkGraphJsonParkExportData data,
        IReadOnlySet<ParkGraphExportSection> sections,
        DateTime exportedAtUtc,
        string metadataSource)
    {
        Park park = data.Park;
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

        if (sections.Contains(ParkGraphExportSection.References))
        {
            document["references"] = data.References ?? new ParkGraphExportReferences();
        }

        if (sections.Contains(ParkGraphExportSection.Zones))
        {
            document["zones"] = data.Zones
                .OrderBy(static zone => zone.SortOrder)
                .ThenBy(static zone => zone.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static zone => MapZone(zone))
                .ToList();
        }

        if (sections.Contains(ParkGraphExportSection.Items))
        {
            document["items"] = data.Items
                .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static item => MapItem(item))
                .ToList();
        }

        if (sections.Contains(ParkGraphExportSection.Images))
        {
            document["images"] = data.Images
                .OrderBy(static image => image.OwnerType.ToString(), StringComparer.Ordinal)
                .ThenBy(static image => image.OwnerId, StringComparer.Ordinal)
                .ThenBy(static image => image.OriginalFileName, StringComparer.OrdinalIgnoreCase)
                .Select(image => MapImage(image, park.Id))
                .ToList();
        }

        if (sections.Contains(ParkGraphExportSection.OpeningHours))
        {
            document["openingHours"] = data.OpeningHours is null ? null : MapOpeningHours(data.OpeningHours);
        }

        if (sections.Contains(ParkGraphExportSection.History))
        {
            document["history"] = MapHistory(data.HistoryEvents);
        }

        document["metadata"] = new ParkGraphExportMetadata
        {
            Source = metadataSource,
            ExportedAtUtc = exportedAtUtc,
        };

        return document;
    }

    public static ParkGraphExportReferences BuildReferences(
        ParkFounder? founder,
        ParkOperator? parkOperator,
        IReadOnlyCollection<AttractionManufacturer> manufacturers)
    {
        return new ParkGraphExportReferences
        {
            Founders = founder is null
                ? new List<ParkGraphExportFounder>()
                : new List<ParkGraphExportFounder> { MapFounder(founder) },
            Operators = parkOperator is null
                ? new List<ParkGraphExportOperator>()
                : new List<ParkGraphExportOperator> { MapOperator(parkOperator) },
            Manufacturers = manufacturers
                .OrderBy(static manufacturer => manufacturer.Id, StringComparer.Ordinal)
                .Select(static manufacturer => MapManufacturer(manufacturer))
                .ToList(),
        };
    }

    public static List<string> BuildDistinctIds(IEnumerable<string?> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToList();
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
                    Labels = CopyLocalizedTexts(rule.Labels),
                    Reasons = CopyLocalizedTexts(rule.Reasons),
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
                    Labels = CopyLocalizedTexts(dateOverride.Labels),
                    Reasons = CopyLocalizedTexts(dateOverride.Reasons),
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

    private static ParkGraphExportHistory MapHistory(IReadOnlyCollection<HistoryEvent> historyEvents)
    {
        return new ParkGraphExportHistory
        {
            Events = historyEvents
                .Select(static historyEvent => MapHistoryEvent(historyEvent))
                .ToList(),
        };
    }

    private static ParkGraphExportHistoryEvent MapHistoryEvent(HistoryEvent historyEvent)
    {
        bool isParkItemEvent = historyEvent.EntityType == HistoryEntityType.ParkItem;
        string? parkItemKey = isParkItemEvent ? historyEvent.OwnerId : null;

        return new ParkGraphExportHistoryEvent
        {
            Key = historyEvent.Key,
            EntityType = historyEvent.EntityType,
            Owner = isParkItemEvent ? "parkItem" : "park",
            OwnerId = string.Empty,
            ParkId = null,
            ParkItemId = null,
            ItemKey = parkItemKey,
            ParkItemKey = parkItemKey,
            ContextParkId = null,
            Year = historyEvent.Year,
            Month = historyEvent.Month,
            Day = historyEvent.Day,
            DatePrecision = historyEvent.DatePrecision,
            EventType = historyEvent.EventType,
            IsMajor = historyEvent.IsMajor,
            IsVisible = historyEvent.IsVisible,
            Slug = historyEvent.Slug,
            Titles = CopyLocalizedTexts(historyEvent.Titles),
            Summaries = CopyLocalizedTexts(historyEvent.Summaries),
            MainImageId = historyEvent.MainImageId,
            PreviousName = historyEvent.PreviousName,
            NewName = historyEvent.NewName,
            PreviousLogoImageId = historyEvent.PreviousLogoImageId,
            NewLogoImageId = historyEvent.NewLogoImageId,
            PreviousOperatorId = historyEvent.PreviousOperatorId,
            NewOperatorId = historyEvent.NewOperatorId,
            LocationLabel = historyEvent.LocationLabel,
            RelatedParkIds = historyEvent.RelatedParkIds.ToList(),
            RelatedParkItemIds = historyEvent.RelatedParkItemIds.ToList(),
            Sources = historyEvent.Sources.Select(static source => MapHistorySource(source)).ToList(),
            Article = historyEvent.Article is null ? null : MapHistoryArticle(historyEvent.Article),
        };
    }

    private static ParkGraphExportHistoryArticle MapHistoryArticle(HistoryArticle article)
    {
        return new ParkGraphExportHistoryArticle
        {
            Slug = article.Slug,
            Titles = CopyLocalizedTexts(article.Titles),
            Subtitles = CopyLocalizedTexts(article.Subtitles),
            Summaries = CopyLocalizedTexts(article.Summaries),
            MainImageId = article.MainImageId,
            Blocks = article.Blocks
                .OrderBy(static block => block.SortOrder)
                .Select(static block => MapHistoryArticleBlock(block))
                .ToList(),
            Sources = article.Sources.Select(static source => MapHistorySource(source)).ToList(),
            IsPublished = article.IsPublished,
        };
    }

    private static ParkGraphExportHistoryArticleBlock MapHistoryArticleBlock(HistoryArticleBlock block)
    {
        return new ParkGraphExportHistoryArticleBlock
        {
            Id = block.Id,
            Type = block.Type,
            SortOrder = block.SortOrder,
            HeadingLevel = block.HeadingLevel,
            Texts = CopyLocalizedTexts(block.Texts),
            ImageId = block.ImageId,
            ImageIds = block.ImageIds.ToList(),
            Captions = CopyLocalizedTexts(block.Captions),
        };
    }

    private static ParkGraphExportHistorySource MapHistorySource(HistorySourceReference source)
    {
        return new ParkGraphExportHistorySource
        {
            Label = source.Label,
            Url = source.Url,
            AccessedAt = source.AccessedAt,
        };
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

    private static string FormatOpeningHoursDate(DateOnly date)
    {
        return date.ToString(OpeningHoursDateFormat, CultureInfo.InvariantCulture);
    }

    private static string FormatOpeningHoursTime(TimeOnly time)
    {
        return time.ToString(OpeningHoursTimeFormat, CultureInfo.InvariantCulture);
    }
}
