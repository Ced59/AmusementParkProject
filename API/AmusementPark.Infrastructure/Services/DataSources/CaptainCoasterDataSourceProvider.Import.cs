using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Results;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;

namespace AmusementPark.Infrastructure.Services.DataSources;

internal sealed partial class CaptainCoasterDataSourceProvider : IDataSourceProvider, IDataSourceImportExecutor
{
    private static readonly IReadOnlyCollection<string> JsonImportSteps = new[] { "ParsingParks", "ParsingCoasters", "BuildComparison" };
    private static readonly IReadOnlyCollection<string> ScrapingImportSteps = new[] { "DiscoverUrls", "FetchCoasters", "EnrichParkCoordinates", "BuildComparison" };

    private static CaptainCoasterImportFiles ResolveInputFiles(DataSourceImportDescriptor importDescriptor)
    {
        if (!string.Equals(importDescriptor.ImportKind, "json-files", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Le mode d'import supporté ici est 'json-files'.", nameof(importDescriptor));
        }

        string parksFilePath = GetRequiredFile(importDescriptor.Files, new[] { "parks", "parksFile", "detected-parks.json", "detected-parks" });
        string coastersFilePath = GetRequiredFile(importDescriptor.Files, new[] { "coasters", "coastersFile", "coasters.json", "coasters" });
        return new CaptainCoasterImportFiles(parksFilePath, coastersFilePath);
    }

    private static string NormalizeImportKind(string? importKind)
    {
        if (string.IsNullOrWhiteSpace(importKind))
        {
            return "sitemap";
        }

        string normalized = importKind.Trim();
        if (string.Equals(normalized, "manual", StringComparison.OrdinalIgnoreCase))
        {
            return "manual-urls";
        }

        return normalized;
    }

    private static bool IsSupportedImportKind(string importKind)
    {
        return string.Equals(importKind, "json-files", StringComparison.OrdinalIgnoreCase)
            || string.Equals(importKind, "sitemap", StringComparison.OrdinalIgnoreCase)
            || string.Equals(importKind, "manual-urls", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<string> GetAvailableSteps(string importKind)
    {
        return string.Equals(importKind, "json-files", StringComparison.OrdinalIgnoreCase)
            ? JsonImportSteps
            : ScrapingImportSteps;
    }

    private static string GetRequiredFile(IReadOnlyCollection<DataSourceInputFileDescriptor> files, IReadOnlyCollection<string> acceptedKeys)
    {
        DataSourceInputFileDescriptor? match = files.FirstOrDefault(file =>
            acceptedKeys.Any(key => string.Equals(file.Key, key, StringComparison.OrdinalIgnoreCase))
            || acceptedKeys.Any(key => string.Equals(file.OriginalFileName, key, StringComparison.OrdinalIgnoreCase)));

        if (match is null || string.IsNullOrWhiteSpace(match.StoredFilePath) || !File.Exists(match.StoredFilePath))
        {
            throw new ArgumentException($"Fichier requis introuvable pour les clés : {string.Join(", ", acceptedKeys)}.", nameof(files));
        }

        return match.StoredFilePath;
    }

    private static void DeleteWorkingDirectorySafe(string? workingDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(workingDirectoryPath))
        {
            return;
        }

        try
        {
            if (Directory.Exists(workingDirectoryPath))
            {
                Directory.Delete(workingDirectoryPath, true);
            }
        }
        catch
        {
        }
    }

    private static DataSourceSettingsResult MapSettings(CaptainCoasterSettingsDocument settings)
    {
        Dictionary<string, string?> options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["baseUrl"] = settings.BaseUrl,
            ["apiKey"] = settings.ApiKey,
            ["dataDirectoryPath"] = settings.DataDirectoryPath,
            ["htmlDirectoryPath"] = settings.HtmlDirectoryPath,
            ["useOfflineMode"] = settings.UseOfflineMode.ToString(),
            ["sitemapUrl"] = settings.SitemapUrl ?? "https://captaincoaster.com/sitemap.xml",
            ["mapPageUrl"] = settings.MapPageUrl ?? "https://captaincoaster.com/fr/map/",
            ["delayBetweenRequestsMs"] = settings.DelayBetweenRequestsMs.ToString(CultureInfo.InvariantCulture),
            ["httpTimeoutSeconds"] = settings.HttpTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
            ["maxRetryCount"] = settings.MaxRetryCount.ToString(CultureInfo.InvariantCulture),
            ["maxConcurrentRequests"] = settings.MaxConcurrentRequests.ToString(CultureInfo.InvariantCulture),
            ["coasterWriteBatchSize"] = settings.CoasterWriteBatchSize.ToString(CultureInfo.InvariantCulture),
            ["progressSaveInterval"] = settings.ProgressSaveInterval.ToString(CultureInfo.InvariantCulture),
            ["maxCoasterCount"] = settings.MaxCoasterCount?.ToString(CultureInfo.InvariantCulture),
            ["skipCoasterCount"] = settings.SkipCoasterCount.ToString(CultureInfo.InvariantCulture),
            ["enrichParkCoordinates"] = settings.EnrichParkCoordinates.ToString(),
            ["mapMarkersAttributeName"] = settings.MapMarkersAttributeName,
            ["coasterTitleXPath"] = settings.CoasterTitleXPath,
            ["characteristicsItemXPath"] = settings.CharacteristicsItemXPath,
            ["characteristicLabelXPath"] = settings.CharacteristicLabelXPath,
            ["characteristicValueXPath"] = settings.CharacteristicValueXPath,
            ["topMetricXPath"] = settings.TopMetricXPath,
        };

        return new DataSourceSettingsResult
        {
            SourceKey = SourceKeyValue,
            DisplayName = DisplayNameValue,
            IsEnabled = settings.IsEnabled,
            Options = options,
        };
    }

    private static string? GetOption(IReadOnlyDictionary<string, string?> options, string key)
    {
        if (options.TryGetValue(key, out string? value))
        {
            return value;
        }

        return null;
    }

    private static bool TryParseBool(string? value)
    {
        return bool.TryParse(value, out bool parsed) && parsed;
    }

    private static int? TryParseInt(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        return null;
    }

    private static List<CaptainCoasterParkSnapshotDocument> ParseParksFromJson(string sessionId, byte[] jsonBytes)
    {
        List<CaptainCoasterParkSnapshotDocument> result = new List<CaptainCoasterParkSnapshotDocument>();
        JsonDocument document = JsonDocument.Parse(jsonBytes);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Le fichier detected-parks.json doit être un tableau JSON.");
        }

        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            string externalId = ReadString(element, "externalId") ?? string.Empty;
            string name = ReadString(element, "name") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            string? countryRaw = ReadString(element, "country");
            result.Add(new CaptainCoasterParkSnapshotDocument
            {
                SourceKey = SourceKeyValue,
                SyncSessionId = sessionId,
                CaptainCoasterId = externalId.Trim(),
                Name = name.Trim(),
                Slug = ReadString(element, "slug"),
                SourceUrl = ReadString(element, "sourceUrl"),
                CountryRaw = countryRaw,
                CountryCode = CountryNameMapper.ToCountryCode(countryRaw),
                Latitude = ReadDouble(element, "latitude"),
                Longitude = ReadDouble(element, "longitude"),
                CoasterCount = ReadInt(element, "coasterCount") ?? 0,
                SampleCoasterNames = ReadStringArray(element, "sampleCoasterNames"),
                ScrapedAtUtc = ReadDateTime(element, "scrapedAtUtc"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        return result;
    }

    private static List<CaptainCoasterCoasterSnapshotDocument> ParseCoastersFromJson(string sessionId, byte[] jsonBytes)
    {
        List<CaptainCoasterCoasterSnapshotDocument> result = new List<CaptainCoasterCoasterSnapshotDocument>();
        JsonDocument document = JsonDocument.Parse(jsonBytes);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Le fichier coasters.json doit être un tableau JSON.");
        }

        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            string externalId = ReadString(element, "externalId") ?? string.Empty;
            string name = ReadString(element, "name") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            result.Add(new CaptainCoasterCoasterSnapshotDocument
            {
                SourceKey = SourceKeyValue,
                SyncSessionId = sessionId,
                CaptainCoasterId = externalId.Trim(),
                Name = name.Trim(),
                Slug = ReadString(element, "slug"),
                SourceUrl = ReadString(element, "sourceUrl"),
                ParkCaptainCoasterId = ReadString(element, "parkSlug"),
                ParkName = ReadString(element, "parkName"),
                Manufacturer = NormalizeManufacturer(ReadString(element, "manufacturer")),
                Model = ReadString(element, "model"),
                MaterialType = ReadString(element, "materialType"),
                SeatingType = ReadString(element, "seatingType"),
                LaunchType = ReadString(element, "launchType"),
                Restraint = ReadString(element, "restraintType"),
                IsLaunched = ReadBool(element, "isLaunched") ?? false,
                HeightInMeters = ReadDouble(element, "heightInMeters"),
                LengthInMeters = ReadDouble(element, "lengthInMeters"),
                SpeedInKmH = ReadDouble(element, "speedInKmH"),
                DropInMeters = null,
                InversionCount = ReadInt(element, "inversionCount"),
                Status = ReadString(element, "status"),
                OpeningDate = PartialDateParser.Parse(ReadString(element, "openingDateText")),
                ClosingDate = PartialDateParser.Parse(ReadString(element, "closingDateText")),
                ScrapedAtUtc = ReadDateTime(element, "scrapedAtUtc"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        return result;
    }

    private static string? NormalizeManufacturer(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        if (string.Equals(trimmed, "Inconnu", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return trimmed;
    }

    private sealed record CaptainCoasterImportFiles(string ParksFilePath, string CoastersFilePath);
}
