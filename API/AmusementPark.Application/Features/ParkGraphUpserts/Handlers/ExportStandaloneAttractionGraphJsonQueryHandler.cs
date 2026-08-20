using System.Text.Json;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;

public sealed partial class ExportParkGraphJsonQueryHandler
{
    public async Task<ApplicationResult<ParkGraphJsonExportResult>> HandleAsync(
        ExportStandaloneAttractionGraphJsonQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.StandaloneAttractionId))
        {
            return ApplicationResult<ParkGraphJsonExportResult>.Failure(ApplicationErrors.Required("standaloneAttractionId"));
        }

        if (this.standaloneAttractionRepository is null)
        {
            return ApplicationResult<ParkGraphJsonExportResult>.Failure(ApplicationErrors.Required("standaloneAttractionRepository"));
        }

        StandaloneAttraction? attraction = await this.standaloneAttractionRepository.GetByIdAsync(query.StandaloneAttractionId.Trim(), true, cancellationToken);
        if (attraction is null)
        {
            return ApplicationResult<ParkGraphJsonExportResult>.Failure(ApplicationErrors.EntityNotFound(nameof(StandaloneAttraction), query.StandaloneAttractionId));
        }

        Task<IReadOnlyCollection<Image>> imagesTask = string.IsNullOrWhiteSpace(attraction.Id)
            ? Task.FromResult<IReadOnlyCollection<Image>>(Array.Empty<Image>())
            : this.imageRepository.GetByOwnersAsync(ImageOwnerType.StandaloneAttraction, new[] { attraction.Id }, null, cancellationToken);
        Task<IReadOnlyCollection<HistoryEvent>> historyEventsTask = this.historyEventRepository is null || string.IsNullOrWhiteSpace(attraction.Id)
            ? Task.FromResult<IReadOnlyCollection<HistoryEvent>>(Array.Empty<HistoryEvent>())
            : this.historyEventRepository.GetOwnerTimelineAsync(
                HistoryEntityType.StandaloneAttraction,
                attraction.Id,
                true,
                cancellationToken);

        await Task.WhenAll(imagesTask, historyEventsTask);
        IReadOnlyCollection<Image> images = await imagesTask;
        IReadOnlyCollection<HistoryEvent> historyEvents = await historyEventsTask;

        DateTime exportedAtUtc = DateTime.UtcNow;
        Dictionary<string, object?> document = new Dictionary<string, object?>
        {
            ["documentType"] = "standaloneAttractionGraph",
            ["schemaVersion"] = "2026-07-16",
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
            ["standaloneAttraction"] = MapStandaloneAttraction(attraction),
            ["images"] = images
                .OrderBy(static image => image.IsCurrent ? 0 : 1)
                .ThenBy(static image => image.OriginalFileName, StringComparer.OrdinalIgnoreCase)
                .Select(MapStandaloneAttractionImage)
                .ToList(),
            ["history"] = MapHistory(historyEvents),
            ["metadata"] = new
            {
                exportedAtUtc,
            },
        };

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

        byte[] content = JsonSerializer.SerializeToUtf8Bytes(document, ExportJsonOptions);
        return ApplicationResult<ParkGraphJsonExportResult>.Success(new ParkGraphJsonExportResult
        {
            FileName = BuildStandaloneAttractionFileName(attraction, exportedAtUtc),
            Content = content,
        });
    }

    private static object MapStandaloneAttraction(StandaloneAttraction attraction)
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
            operatorKey = (string?)null,
            websiteUrl = attraction.WebsiteUrl,
            street = attraction.Street,
            city = attraction.City,
            postalCode = attraction.PostalCode,
            descriptions = CopyLocalizedTexts(attraction.Descriptions),
            attractionDetails = attraction.AttractionDetails is null ? null : MapAttractionDetails(attraction.AttractionDetails, false),
            attractionLocations = attraction.AttractionLocations,
            isVisible = attraction.IsVisible,
            adminReviewStatus = attraction.AdminReviewStatus,
            legacyParkId = attraction.LegacyParkId,
            legacyParkItemId = attraction.LegacyParkItemId,
            latitude = attraction.Position?.Latitude,
            longitude = attraction.Position?.Longitude,
        };
    }

    private static object MapStandaloneAttractionImage(Image image)
    {
        return new
        {
            imageId = image.Id,
            id = image.Id,
            ownerType = ImageOwnerType.StandaloneAttraction,
            ownerId = image.OwnerId,
            ownerKey = "standaloneAttraction",
            category = image.Category == ImageCategory.Park || image.Category == ImageCategory.ParkItem
                ? ImageCategory.StandaloneAttraction
                : image.Category,
            isPublished = image.IsPublished,
            isCurrent = image.IsCurrent,
            setAsCurrent = image.IsCurrent,
            withWatermark = false,
            isWatermarked = image.IsWatermarked,
            sourceUrl = image.SourceUrl,
            internalUrl = BuildInternalImageUrl(image.Id),
            description = image.Description,
            altTexts = CopyLocalizedTexts(image.AltTexts),
            captions = CopyLocalizedTexts(image.Captions),
            credits = CopyLocalizedTexts(image.Credits),
            tagIds = image.TagIds.ToList(),
            geoLocation = image.GeoLocation,
            originalFileName = image.OriginalFileName,
            contentType = image.ContentType,
            width = image.Width,
            height = image.Height,
            sizeInBytes = image.SizeInBytes,
        };
    }

    private static string BuildStandaloneAttractionFileName(StandaloneAttraction attraction, DateTime exportedAtUtc)
    {
        string baseName = string.IsNullOrWhiteSpace(attraction.Name) ? attraction.Id ?? "standalone-attraction" : attraction.Name;
        string slug = SanitizeFileName(baseName);
        return $"{slug}-standalone-attraction-upsert-{exportedAtUtc:yyyyMMdd-HHmmss}.json";
    }
}
