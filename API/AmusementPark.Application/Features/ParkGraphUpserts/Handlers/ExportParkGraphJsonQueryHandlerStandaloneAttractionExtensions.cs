using System.Globalization;
using System.Text;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkPricing.Ports;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Application.Common.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
internal static class ExportParkGraphJsonQueryHandlerStandaloneAttractionExtensions
{
    internal static async Task<ApplicationResult<ParkGraphJsonExportResult>> HandleAsync(this ExportParkGraphJsonQueryHandler processorContext, ExportStandaloneAttractionGraphJsonQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.StandaloneAttractionId))
        {
            return ApplicationResult<ParkGraphJsonExportResult>.Failure(ApplicationErrors.Required("standaloneAttractionId"));
        }

        if (processorContext.standaloneAttractionRepository is null)
        {
            return ApplicationResult<ParkGraphJsonExportResult>.Failure(ApplicationErrors.Required("standaloneAttractionRepository"));
        }

        StandaloneAttraction? attraction = await processorContext.standaloneAttractionRepository.GetByIdAsync(query.StandaloneAttractionId.Trim(), true, cancellationToken);
        if (attraction is null)
        {
            return ApplicationResult<ParkGraphJsonExportResult>.Failure(ApplicationErrors.EntityNotFound(nameof(StandaloneAttraction), query.StandaloneAttractionId));
        }

        Task<IReadOnlyCollection<Image>> imagesTask = string.IsNullOrWhiteSpace(attraction.Id) ? Task.FromResult<IReadOnlyCollection<Image>>(Array.Empty<Image>()) : processorContext.imageRepository.GetByOwnersAsync(ImageOwnerType.StandaloneAttraction, new[] { attraction.Id }, null, cancellationToken);
        Task<IReadOnlyCollection<HistoryEvent>> historyEventsTask = processorContext.historyEventRepository is null || string.IsNullOrWhiteSpace(attraction.Id) ? Task.FromResult<IReadOnlyCollection<HistoryEvent>>(Array.Empty<HistoryEvent>()) : processorContext.historyEventRepository.GetOwnerTimelineAsync(HistoryEntityType.StandaloneAttraction, attraction.Id, true, cancellationToken);
        bool exportsVisitorInformation =
            ParkItemStatusNormalizer.IsOperating(attraction.AttractionDetails?.Status);
        Task<ParkOpeningHoursSchedule?> openingHoursTask =
            processorContext.standaloneOpeningHoursRepository is null || !exportsVisitorInformation
                ? Task.FromResult<ParkOpeningHoursSchedule?>(null)
                : processorContext.standaloneOpeningHoursRepository.GetByStandaloneAttractionIdAsync(
                    attraction.Id,
                    cancellationToken);
        Task<ParkPricingEntity?> pricingTask =
            processorContext.standalonePricingRepository is null || !exportsVisitorInformation
                ? Task.FromResult<ParkPricingEntity?>(null)
                : processorContext.standalonePricingRepository.GetByStandaloneAttractionIdAsync(
                    attraction.Id,
                    cancellationToken);
        await Task.WhenAll(imagesTask, historyEventsTask, openingHoursTask, pricingTask);
        IReadOnlyCollection<Image> images = await imagesTask;
        IReadOnlyCollection<HistoryEvent> historyEvents = await historyEventsTask;
        ParkOpeningHoursSchedule? openingHours = await openingHoursTask;
        ParkPricingEntity? pricing = await pricingTask;
        DateTime exportedAtUtc = DateTime.UtcNow;
        Dictionary<string, object?> document = new Dictionary<string, object?>
        {
            ["documentType"] = "standaloneAttractionGraph",
            ["schemaVersion"] = "2026-09-30",
            ["mode"] = "merge",
            ["identity"] = new
            {
                standaloneAttractionId = attraction.Id,
                id = attraction.Id,
                name = attraction.Name,
                countryCode = attraction.CountryCode,
                legacyParkId = attraction.LegacyParkId,
                legacyParkItemId = attraction.LegacyParkItemId,
            },
            ["standaloneAttraction"] = ExportParkGraphJsonQueryHandlerStandaloneAttractionExtensions.MapStandaloneAttraction(attraction),
            ["images"] = images.OrderBy(static image => image.IsCurrent ? 0 : 1).ThenBy(static image => image.OriginalFileName, StringComparer.OrdinalIgnoreCase).Select(ExportParkGraphJsonQueryHandlerStandaloneAttractionExtensions.MapStandaloneAttractionImage).ToList(),
            ["history"] = ExportParkGraphJsonQueryHandlerHistoryMappingExtensions.MapHistory(historyEvents),
            ["metadata"] = new
            {
                exportedAtUtc,
            },
        };
        if (openingHours is not null)
        {
            document["openingHours"] =
                ExportParkGraphJsonQueryHandlerStandaloneAttractionExtensions.MapStandaloneOpeningHours(openingHours);
        }

        if (pricing is not null)
        {
            document["pricing"] =
                ExportParkGraphJsonQueryHandlerStandaloneAttractionExtensions.MapStandalonePricing(pricing);
        }

        if (!string.IsNullOrWhiteSpace(attraction.LegacyParkId))
        {
            document["migration"] = new
            {
                legacyParkId = attraction.LegacyParkId,
                legacyParkItemId = attraction.LegacyParkItemId,
                targetStandaloneAttractionId = attraction.Id,
                retireLegacyPark = false,
                retireLegacyParkItem = false,
            };
        }

        byte[] content = JsonSerializer.SerializeToUtf8Bytes(document, ExportParkGraphJsonQueryHandler.ExportJsonOptions);
        return ApplicationResult<ParkGraphJsonExportResult>.Success(new ParkGraphJsonExportResult { FileName = ExportParkGraphJsonQueryHandlerStandaloneAttractionExtensions.BuildStandaloneAttractionFileName(attraction, exportedAtUtc), Content = content, });
    }

    internal static object MapStandaloneAttraction(StandaloneAttraction attraction)
    {
        return new
        {
            key = attraction.Id,
            id = attraction.Id,
            name = attraction.Name,
            countryCode = attraction.CountryCode,
            type = attraction.Type,
            subtype = attraction.Subtype,
            operatorId = attraction.OperatorId,
            operatorKey = (string? )null,
            websiteUrl = attraction.WebsiteUrl,
            street = attraction.Street,
            city = attraction.City,
            postalCode = attraction.PostalCode,
            descriptions = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(attraction.Descriptions),
            attractionDetails = attraction.AttractionDetails is null ? null : ExportParkGraphJsonQueryHandler.MapAttractionDetails(attraction.AttractionDetails, false),
            attractionLocations = attraction.AttractionLocations,
            isVisible = attraction.IsVisible,
            adminReviewStatus = attraction.AdminReviewStatus,
            legacyParkId = attraction.LegacyParkId,
            legacyParkItemId = attraction.LegacyParkItemId,
            latitude = attraction.Position?.Latitude,
            longitude = attraction.Position?.Longitude,
        };
    }

    internal static object MapStandaloneAttractionImage(Image image)
    {
        return new
        {
            imageId = image.Id,
            id = image.Id,
            ownerType = ImageOwnerType.StandaloneAttraction,
            ownerId = image.OwnerId,
            ownerKey = "standaloneAttraction",
            category = image.Category == ImageCategory.Park || image.Category == ImageCategory.ParkItem ? ImageCategory.StandaloneAttraction : image.Category,
            isPublished = image.IsPublished,
            isCurrent = image.IsCurrent,
            setAsCurrent = image.IsCurrent,
            withWatermark = false,
            isWatermarked = image.IsWatermarked,
            sourceUrl = image.SourceUrl,
            internalUrl = ExportParkGraphJsonQueryHandlerMappingExtensions.BuildInternalImageUrl(image.Id),
            description = image.Description,
            altTexts = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(image.AltTexts),
            captions = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(image.Captions),
            credits = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(image.Credits),
            tagIds = image.TagIds.ToList(),
            geoLocation = image.GeoLocation,
            originalFileName = image.OriginalFileName,
            contentType = image.ContentType,
            width = image.Width,
            height = image.Height,
            sizeInBytes = image.SizeInBytes,
        };
    }

    internal static object MapStandaloneOpeningHours(ParkOpeningHoursSchedule schedule)
    {
        ParkGraphExportOpeningHours mapped =
            ExportParkGraphJsonQueryHandlerMappingExtensions.MapOpeningHours(schedule);
        return new
        {
            standaloneAttractionId = schedule.ParkId,
            mapped.TimeZoneId,
            mapped.SourceUrl,
            mapped.Notes,
            mapped.LastVerifiedAtUtc,
            mapped.RegularRules,
            mapped.DateOverrides,
        };
    }

    internal static object MapStandalonePricing(ParkPricingEntity pricing)
    {
        ParkGraphExportPricing mapped = ParkGraphPricingExportMapper.Map(pricing);
        return new
        {
            standaloneAttractionId = pricing.ParkId,
            mapped.CurrencyCode,
            mapped.SourceUrl,
            mapped.PurchaseUrl,
            mapped.Notes,
            mapped.LastVerifiedAtUtc,
            mapped.AdmissionOffers,
            mapped.AnnualPasses,
            mapped.ParkingOffers,
            mapped.CreditOffers,
            mapped.HistoricalSnapshots,
        };
    }

    internal static string BuildStandaloneAttractionFileName(StandaloneAttraction attraction, DateTime exportedAtUtc)
    {
        string baseName = string.IsNullOrWhiteSpace(attraction.Name) ? attraction.Id ?? "standalone-attraction" : attraction.Name;
        string slug = ExportParkGraphJsonQueryHandlerMappingExtensions.SanitizeFileName(baseName);
        return $"{slug}-standalone-attraction-upsert-{exportedAtUtc:yyyyMMdd-HHmmss}.json";
    }
}
