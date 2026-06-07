using System.Globalization;
using System.Text;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Services;

public sealed class ParkItemsBulkCreatePreviewService
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly ParkItemReferenceValidator parkItemReferenceValidator;

    public ParkItemsBulkCreatePreviewService(
        IParkItemRepository parkItemRepository,
        IParkZoneRepository parkZoneRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        ParkItemReferenceValidator parkItemReferenceValidator)
    {
        this.parkItemRepository = parkItemRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.parkItemReferenceValidator = parkItemReferenceValidator;
    }

    public async Task<ApplicationResult<ParkItemsBulkCreatePreviewResult>> PreviewAsync(
        string parkId,
        IReadOnlyCollection<ParkItemBulkCreateDraft> rows,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = NormalizeText(parkId) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedParkId))
        {
            return ApplicationResult<ParkItemsBulkCreatePreviewResult>.Failure(ApplicationErrors.Required(nameof(parkId)));
        }

        if (rows.Count == 0)
        {
            return ApplicationResult<ParkItemsBulkCreatePreviewResult>.Failure(ApplicationErrors.Required(nameof(rows)));
        }

        ApplicationError? parkError = await this.parkItemReferenceValidator.EnsureParkExistsAsync(normalizedParkId, cancellationToken);
        if (parkError is not null)
        {
            return ApplicationResult<ParkItemsBulkCreatePreviewResult>.Failure(parkError);
        }

        IReadOnlyCollection<ParkZone> zones = await this.parkZoneRepository.GetByParkIdAsync(normalizedParkId, cancellationToken);
        IReadOnlyCollection<AttractionManufacturer> manufacturers = await this.attractionManufacturerRepository.GetAllAsync(cancellationToken);
        IReadOnlyCollection<ParkItem> existingItems = await this.parkItemRepository.GetByParkIdAsync(normalizedParkId, true, cancellationToken);

        Dictionary<string, ParkZone> zonesById = zones
            .Where(static zone => !string.IsNullOrWhiteSpace(zone.Id))
            .GroupBy(static zone => zone.Id, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
        Dictionary<string, ParkZone> zonesByName = zones
            .SelectMany(static zone => GetZoneNames(zone).Select(name => new KeyValuePair<string, ParkZone>(NormalizeKey(name), zone)))
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .GroupBy(static pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First().Value, StringComparer.Ordinal);
        Dictionary<string, AttractionManufacturer> manufacturersById = manufacturers
            .Where(static manufacturer => !string.IsNullOrWhiteSpace(manufacturer.Id))
            .GroupBy(static manufacturer => manufacturer.Id, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
        Dictionary<string, AttractionManufacturer> manufacturersByName = manufacturers
            .SelectMany(static manufacturer => GetManufacturerNames(manufacturer).Select(name => new KeyValuePair<string, AttractionManufacturer>(NormalizeKey(name), manufacturer)))
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .GroupBy(static pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First().Value, StringComparer.Ordinal);
        HashSet<string> existingItemNames = existingItems
            .Select(static item => NormalizeKey(item.Name))
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> importedItemNames = new HashSet<string>(StringComparer.Ordinal);

        List<ParkItemBulkCreatePreviewRow> previewRows = new List<ParkItemBulkCreatePreviewRow>();
        foreach (ParkItemBulkCreateDraft row in rows.OrderBy(static item => item.RowNumber))
        {
            previewRows.Add(this.BuildPreviewRow(row, zonesById, zonesByName, manufacturersById, manufacturersByName, existingItemNames, importedItemNames));
        }

        ParkItemsBulkCreatePreviewResult result = new ParkItemsBulkCreatePreviewResult
        {
            Rows = previewRows,
            ReadyCount = previewRows.Count(static row => row.CanApply && row.Warnings.Count == 0),
            WarningCount = previewRows.Count(static row => row.CanApply && row.Warnings.Count > 0),
            ErrorCount = previewRows.Count(static row => !row.CanApply),
        };

        return ApplicationResult<ParkItemsBulkCreatePreviewResult>.Success(result);
    }

    public ParkItem ToParkItem(string parkId, ParkItemBulkCreatePreviewRow row)
    {
        ParkItem parkItem = new ParkItem
        {
            ParkId = parkId,
            ZoneId = row.ZoneId,
            Name = row.Name,
            Category = row.Category,
            Type = row.Type,
            Descriptions = string.IsNullOrWhiteSpace(row.DescriptionFr)
                ? new List<Core.Localization.LocalizedText>()
                : new List<Core.Localization.LocalizedText> { new Core.Localization.LocalizedText("fr", row.DescriptionFr) },
            AttractionDetails = row.Category == ParkItemCategory.Attraction && !string.IsNullOrWhiteSpace(row.ManufacturerId)
                ? new AttractionDetails { ManufacturerId = row.ManufacturerId }
                : null,
            IsVisible = row.IsVisible,
            AdminReviewStatus = row.AdminReviewStatus,
        };

        ParkItemAdministrationDefaults.ApplyQuickCreateDefaults(parkItem);
        ParkItemNormalization.Normalize(parkItem);
        return parkItem;
    }

    private ParkItemBulkCreatePreviewRow BuildPreviewRow(
        ParkItemBulkCreateDraft row,
        IReadOnlyDictionary<string, ParkZone> zonesById,
        IReadOnlyDictionary<string, ParkZone> zonesByName,
        IReadOnlyDictionary<string, AttractionManufacturer> manufacturersById,
        IReadOnlyDictionary<string, AttractionManufacturer> manufacturersByName,
        IReadOnlySet<string> existingItemNames,
        ISet<string> importedItemNames)
    {
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();
        string normalizedName = NormalizeText(row.Name) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errors.Add("name.required");
        }

        ParkItemCategory category = row.Category ?? ParkItemAdministrationDefaults.QuickCreateCategory;
        ParkItemType type = row.Type ?? ParkItemAdministrationDefaults.GetDefaultType(category);
        string? zoneId = this.ResolveZone(row, zonesById, zonesByName, errors, warnings, out string? zoneName);
        string? manufacturerId = this.ResolveManufacturer(row, manufacturersById, manufacturersByName, errors, warnings, out string? manufacturerName);

        string normalizedNameKey = NormalizeKey(normalizedName);
        if (!string.IsNullOrWhiteSpace(normalizedNameKey))
        {
            if (existingItemNames.Contains(normalizedNameKey))
            {
                warnings.Add("duplicate.existing");
            }

            if (!importedItemNames.Add(normalizedNameKey))
            {
                warnings.Add("duplicate.import");
            }
        }

        if (category != ParkItemCategory.Attraction && !string.IsNullOrWhiteSpace(manufacturerId))
        {
            warnings.Add("manufacturer.ignored-for-non-attraction");
            manufacturerId = null;
            manufacturerName = null;
        }

        return new ParkItemBulkCreatePreviewRow
        {
            RowNumber = row.RowNumber,
            Name = normalizedName,
            Category = category,
            Type = type,
            ZoneId = zoneId,
            ZoneName = zoneName,
            ManufacturerId = manufacturerId,
            ManufacturerName = manufacturerName,
            IsVisible = row.IsVisible ?? ParkItemAdministrationDefaults.QuickCreateIsVisible,
            AdminReviewStatus = row.AdminReviewStatus ?? ParkItemAdministrationDefaults.QuickCreateAdminReviewStatus,
            DescriptionFr = NormalizeText(row.DescriptionFr),
            CanApply = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
        };
    }

    private string? ResolveZone(
        ParkItemBulkCreateDraft row,
        IReadOnlyDictionary<string, ParkZone> zonesById,
        IReadOnlyDictionary<string, ParkZone> zonesByName,
        ICollection<string> errors,
        ICollection<string> warnings,
        out string? zoneName)
    {
        zoneName = null;
        string? zoneId = NormalizeText(row.ZoneId);
        if (!string.IsNullOrWhiteSpace(zoneId))
        {
            if (zonesById.TryGetValue(zoneId, out ParkZone? zone))
            {
                zoneName = GetZoneNames(zone).FirstOrDefault();
                return zone.Id;
            }

            errors.Add("zone.unknown");
            return null;
        }

        string? requestedZoneName = NormalizeText(row.ZoneName);
        if (string.IsNullOrWhiteSpace(requestedZoneName))
        {
            return null;
        }

        string zoneNameKey = NormalizeKey(requestedZoneName);
        if (zonesByName.TryGetValue(zoneNameKey, out ParkZone? resolvedZone))
        {
            zoneName = GetZoneNames(resolvedZone).FirstOrDefault();
            warnings.Add("zone.resolved-by-name");
            return resolvedZone.Id;
        }

        errors.Add("zone.unknown");
        return null;
    }

    private string? ResolveManufacturer(
        ParkItemBulkCreateDraft row,
        IReadOnlyDictionary<string, AttractionManufacturer> manufacturersById,
        IReadOnlyDictionary<string, AttractionManufacturer> manufacturersByName,
        ICollection<string> errors,
        ICollection<string> warnings,
        out string? manufacturerName)
    {
        manufacturerName = null;
        string? manufacturerId = NormalizeText(row.ManufacturerId);
        if (!string.IsNullOrWhiteSpace(manufacturerId))
        {
            if (manufacturersById.TryGetValue(manufacturerId, out AttractionManufacturer? manufacturer))
            {
                manufacturerName = manufacturer.Name;
                return manufacturer.Id;
            }

            errors.Add("manufacturer.unknown");
            return null;
        }

        string? requestedManufacturerName = NormalizeText(row.ManufacturerName);
        if (string.IsNullOrWhiteSpace(requestedManufacturerName))
        {
            return null;
        }

        string manufacturerNameKey = NormalizeKey(requestedManufacturerName);
        if (manufacturersByName.TryGetValue(manufacturerNameKey, out AttractionManufacturer? resolvedManufacturer))
        {
            manufacturerName = resolvedManufacturer.Name;
            warnings.Add("manufacturer.resolved-by-name");
            return resolvedManufacturer.Id;
        }

        errors.Add("manufacturer.unknown");
        return null;
    }

    private static IEnumerable<string> GetZoneNames(ParkZone zone)
    {
        if (!string.IsNullOrWhiteSpace(zone.Name))
        {
            yield return zone.Name;
        }

        foreach (Core.Localization.LocalizedText localizedName in zone.Names)
        {
            if (!string.IsNullOrWhiteSpace(localizedName.Value))
            {
                yield return localizedName.Value;
            }
        }
    }

    private static IEnumerable<string> GetManufacturerNames(AttractionManufacturer manufacturer)
    {
        if (!string.IsNullOrWhiteSpace(manufacturer.Name))
        {
            yield return manufacturer.Name;
        }

        if (!string.IsNullOrWhiteSpace(manufacturer.LegalName))
        {
            yield return manufacturer.LegalName;
        }
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.Trim().Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(normalized.Length);
        foreach (char character in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
