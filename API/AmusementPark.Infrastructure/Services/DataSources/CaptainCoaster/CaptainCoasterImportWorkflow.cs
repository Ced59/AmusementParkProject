using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Services.DataSources.Acquisition;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using System.Globalization;
using System.Threading.Channels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AmusementPark.Application.Features.Search;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;

internal static class CaptainCoasterImportWorkflow
{
    private static readonly IReadOnlyCollection<string> JsonImportSteps = new[] { "ParsingParks", "ParsingCoasters", "BuildComparison" };
    private static readonly IReadOnlyCollection<string> ScrapingImportSteps = new[] { "DiscoverUrls", "FetchCoasters", "EnrichParkCoordinates", "BuildComparison" };

    internal static CaptainCoasterImportFiles ResolveInputFiles(this CaptainCoasterDataSourceProvider provider, DataSourceImportDescriptor importDescriptor)
    {
        if (!string.Equals(importDescriptor.ImportKind, "json-files", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Le mode d'import supporté ici est 'json-files'.", nameof(importDescriptor));
        }

        string parksFilePath = provider.GetRequiredFile(importDescriptor.Files, new[] { "parks", "parksFile", "detected-parks.json", "detected-parks" });
        string coastersFilePath = provider.GetRequiredFile(importDescriptor.Files, new[] { "coasters", "coastersFile", "coasters.json", "coasters" });
        return new CaptainCoasterImportFiles(parksFilePath, coastersFilePath);
    }

    internal static string NormalizeImportKind(this CaptainCoasterDataSourceProvider provider, string? importKind)
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

    internal static bool IsSupportedImportKind(this CaptainCoasterDataSourceProvider provider, string importKind)
    {
        return string.Equals(importKind, "json-files", StringComparison.OrdinalIgnoreCase)
            || string.Equals(importKind, "sitemap", StringComparison.OrdinalIgnoreCase)
            || string.Equals(importKind, "manual-urls", StringComparison.OrdinalIgnoreCase);
    }

    internal static IReadOnlyCollection<string> GetAvailableSteps(this CaptainCoasterDataSourceProvider provider, string importKind)
    {
        return string.Equals(importKind, "json-files", StringComparison.OrdinalIgnoreCase)
            ? JsonImportSteps
            : ScrapingImportSteps;
    }

    internal static string GetRequiredFile(this CaptainCoasterDataSourceProvider provider, IReadOnlyCollection<DataSourceInputFileDescriptor> files, IReadOnlyCollection<string> acceptedKeys)
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

    internal static void DeleteWorkingDirectorySafe(this CaptainCoasterDataSourceProvider provider, string? workingDirectoryPath)
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
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }

    internal static DataSourceSettingsResult MapSettings(this CaptainCoasterDataSourceProvider provider, CaptainCoasterSettingsDocument settings)
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
            SourceKey = CaptainCoasterDataSourceProvider.SourceKeyValue,
            DisplayName = CaptainCoasterDataSourceProvider.DisplayNameValue,
            IsEnabled = settings.IsEnabled,
            Options = options,
        };
    }

    internal static string? GetOption(this CaptainCoasterDataSourceProvider provider, IReadOnlyDictionary<string, string?> options, string key)
    {
        if (options.TryGetValue(key, out string? value))
        {
            return value;
        }

        return null;
    }

    internal static bool TryParseBool(this CaptainCoasterDataSourceProvider provider, string? value)
    {
        return bool.TryParse(value, out bool parsed) && parsed;
    }

    internal static int? TryParseInt(this CaptainCoasterDataSourceProvider provider, string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        return null;
    }

    internal static List<CaptainCoasterParkSnapshotDocument> ParseParksFromJson(this CaptainCoasterDataSourceProvider provider, string sessionId, byte[] jsonBytes)
    {
        List<CaptainCoasterParkSnapshotDocument> result = new List<CaptainCoasterParkSnapshotDocument>();
        JsonDocument document = JsonDocument.Parse(jsonBytes);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Le fichier detected-parks.json doit être un tableau JSON.");
        }

        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            string externalId = provider.ReadString(element, "externalId") ?? string.Empty;
            string name = provider.ReadString(element, "name") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            string? countryRaw = provider.ReadString(element, "country");
            result.Add(new CaptainCoasterParkSnapshotDocument
            {
                SourceKey = CaptainCoasterDataSourceProvider.SourceKeyValue,
                SyncSessionId = sessionId,
                CaptainCoasterId = externalId.Trim(),
                Name = name.Trim(),
                Slug = provider.ReadString(element, "slug"),
                SourceUrl = provider.ReadString(element, "sourceUrl"),
                CountryRaw = countryRaw,
                CountryCode = CountryNameMapper.ToCountryCode(countryRaw),
                Latitude = provider.ReadDouble(element, "latitude"),
                Longitude = provider.ReadDouble(element, "longitude"),
                CoasterCount = provider.ReadInt(element, "coasterCount") ?? 0,
                SampleCoasterNames = provider.ReadStringArray(element, "sampleCoasterNames"),
                ScrapedAtUtc = provider.ReadDateTime(element, "scrapedAtUtc"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        return result;
    }

    internal static List<CaptainCoasterCoasterSnapshotDocument> ParseCoastersFromJson(this CaptainCoasterDataSourceProvider provider, string sessionId, byte[] jsonBytes)
    {
        List<CaptainCoasterCoasterSnapshotDocument> result = new List<CaptainCoasterCoasterSnapshotDocument>();
        JsonDocument document = JsonDocument.Parse(jsonBytes);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Le fichier coasters.json doit être un tableau JSON.");
        }

        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            string externalId = provider.ReadString(element, "externalId") ?? string.Empty;
            string name = provider.ReadString(element, "name") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            string? countryRaw = provider.ReadString(element, "country");
            result.Add(new CaptainCoasterCoasterSnapshotDocument
            {
                SourceKey = CaptainCoasterDataSourceProvider.SourceKeyValue,
                SyncSessionId = sessionId,
                CaptainCoasterId = externalId.Trim(),
                Name = name.Trim(),
                Slug = provider.ReadString(element, "slug"),
                SourceUrl = provider.ReadString(element, "sourceUrl"),
                ParkCaptainCoasterId = provider.ReadString(element, "parkSlug"),
                ParkName = provider.ReadString(element, "parkName"),
                CountryRaw = provider.NormalizeNullableText(countryRaw),
                CountryCode = CountryNameMapper.ToCountryCode(countryRaw),
                Manufacturer = provider.NormalizeManufacturer(provider.ReadString(element, "manufacturer")),
                Model = provider.ReadString(element, "model"),
                MaterialType = provider.ReadString(element, "materialType"),
                SeatingType = provider.ReadString(element, "seatingType"),
                LaunchType = provider.ReadString(element, "launchType"),
                Restraint = provider.ReadString(element, "restraintType"),
                IsLaunched = provider.ReadBool(element, "isLaunched") ?? false,
                HeightInMeters = provider.ReadDouble(element, "heightInMeters"),
                LengthInMeters = provider.ReadDouble(element, "lengthInMeters"),
                SpeedInKmH = provider.ReadDouble(element, "speedInKmH"),
                DropInMeters = null,
                InversionCount = provider.ReadInt(element, "inversionCount"),
                Status = provider.ReadString(element, "status"),
                OpeningDate = PartialDateParser.Parse(provider.ReadString(element, "openingDateText")),
                ClosingDate = PartialDateParser.Parse(provider.ReadString(element, "closingDateText")),
                ScrapedAtUtc = provider.ReadDateTime(element, "scrapedAtUtc"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        return result;
    }

    internal static string? NormalizeManufacturer(this CaptainCoasterDataSourceProvider provider, string? value)
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
}
