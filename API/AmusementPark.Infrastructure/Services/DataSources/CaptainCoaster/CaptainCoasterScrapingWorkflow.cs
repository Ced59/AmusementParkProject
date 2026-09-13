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

internal static class CaptainCoasterScrapingWorkflow
{
    internal static async Task ExecuteScrapingImportAsync(this CaptainCoasterDataSourceProvider provider, DataSourceImportJob job, CaptainCoasterSyncSessionDocument session, CancellationToken cancellationToken)
    {
        CaptainCoasterSettingsDocument settingsDocument = await provider.GetOrCreateSettingsAsync();
        CaptainCoasterScrapingSettings scrapingSettings = provider.BuildScrapingSettings(job.ImportDescriptor, settingsDocument);
        string startAtStep = provider.NormalizeStartStep(provider.GetOption(job.ImportDescriptor.Options, "startAtStep"));

        IReadOnlyCollection<CaptainCoasterDiscoveredUrl> discoveredUrls;
        if (provider.ShouldRunStep(startAtStep, "DiscoverUrls"))
        {
            await provider.UpdateSessionAsync(session, "DiscoverUrls", "Découverte des URLs à analyser.", 5, cancellationToken);
            discoveredUrls = await provider.DiscoverUrlsAsync(job.ImportDescriptor, scrapingSettings, cancellationToken);
            await provider.StageDiscoveredUrlsAsync(session.Id, discoveredUrls, cancellationToken);
            session.DiscoveredUrls = null;
            session.Metrics.DiscoveredItems = discoveredUrls.Count;
            session.LastCompletedStep = "DiscoverUrls";
            provider.AddLog(session, "Info", $"{discoveredUrls.Count} URL(s) retenue(s) pour le traitement.");
            await provider.PersistSessionAsync(session, cancellationToken);
        }
        else
        {
            discoveredUrls = await provider.LoadDiscoveredUrlsAsync(session, scrapingSettings.Language, cancellationToken);

            if (discoveredUrls.Count == 0)
            {
                throw new InvalidOperationException("Aucune URL découverte n'est disponible pour reprendre le workflow depuis cette étape.");
            }
        }

        if (provider.ShouldRunStep(startAtStep, "FetchCoasters"))
        {
            await provider.UpdateSessionAsync(session, "FetchCoasters", "Téléchargement et parsing des pages coaster.", 15, cancellationToken);
            await provider.ProcessCoasterPagesAsync(session, discoveredUrls, scrapingSettings, cancellationToken);
        }

        if (provider.ShouldRunStep(startAtStep, "EnrichParkCoordinates"))
        {
            await provider.UpdateSessionAsync(session, "EnrichParkCoordinates", "Enrichissement des coordonnées de parcs.", 75, cancellationToken);
            await provider.EnrichParkCoordinatesAsync(session, scrapingSettings, cancellationToken);
        }

        if (provider.ShouldRunStep(startAtStep, "BuildComparison"))
        {
            await provider.UpdateSessionAsync(session, "BuildComparison", "Construction du rapport de comparaison.", 90, cancellationToken);
            await provider.BuildComparisonFromStagingAsync(session, cancellationToken);
        }

        settingsDocument.LastSuccessfulSyncUtc = DateTime.UtcNow;
        settingsDocument.UpdatedAt = DateTime.UtcNow;
        await provider.settingsCollection.ReplaceOneAsync(item => item.Id == settingsDocument.Id, settingsDocument, new ReplaceOptions { IsUpsert = true }, cancellationToken);

        session.Status = "Completed";
        session.CurrentStep = "Completed";
        session.Message = "Import Captain Coaster terminé. Les changements sont prêts pour validation manuelle.";
        session.ProgressPercentage = 100;
        session.CompletedAtUtc = DateTime.UtcNow;
        session.CanResume = true;
        session.UpdatedAt = DateTime.UtcNow;
        provider.AddLog(session, "Info", $"Terminé : {session.Metrics.ParksFetched} parc(s), {session.Metrics.CoastersFetched} coaster(s), {session.Metrics.ComparisonResults} résultat(s). Les changements restent en attente de validation manuelle avant intégration métier.");
        await provider.PersistSessionAsync(session, cancellationToken);
    }

    internal static async Task<IReadOnlyCollection<CaptainCoasterDiscoveredUrl>> DiscoverUrlsAsync(this CaptainCoasterDataSourceProvider provider,
        DataSourceImportDescriptor importDescriptor,
        CaptainCoasterScrapingSettings scrapingSettings,
        CancellationToken cancellationToken)
    {
        string importKind = provider.NormalizeImportKind(importDescriptor.ImportKind);
        List<CaptainCoasterDiscoveredUrl> urls = new List<CaptainCoasterDiscoveredUrl>();

        if (string.Equals(importKind, "manual-urls", StringComparison.OrdinalIgnoreCase))
        {
            foreach (string url in importDescriptor.Urls)
            {
                CaptainCoasterDiscoveredUrl? parsed = CaptainCoasterScrapingUrlParser.TryParse(url, scrapingSettings.Language);
                if (parsed is not null)
                {
                    urls.Add(parsed);
                }
            }
        }
        else
        {
            string sitemapContent = await provider.dataAcquisitionHttpFetcher.GetStringAsync(
                scrapingSettings.SitemapUrl,
                scrapingSettings.Language + ";q=1.0,en;q=0.8",
                provider.BuildRequestOptions(scrapingSettings),
                cancellationToken);

            IReadOnlyCollection<string> sitemapUrls = provider.xmlSitemapUrlDiscoveryService.ReadUrls(sitemapContent);
            foreach (string sitemapUrl in sitemapUrls)
            {
                CaptainCoasterDiscoveredUrl? parsed = CaptainCoasterScrapingUrlParser.TryParse(sitemapUrl, scrapingSettings.Language);
                if (parsed is not null)
                {
                    urls.Add(parsed);
                }
            }
        }

        List<CaptainCoasterDiscoveredUrl> orderedUrls = urls
            .GroupBy(static item => item.CaptainCoasterId, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(item => int.TryParse(item.CaptainCoasterId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ? id : int.MaxValue)
            .ThenBy(static item => item.CaptainCoasterId, StringComparer.OrdinalIgnoreCase)
            .Skip(scrapingSettings.SkipCoasterCount)
            .Take(scrapingSettings.MaxCoasterCount ?? int.MaxValue)
            .ToList();

        if (orderedUrls.Count == 0)
        {
            throw new InvalidOperationException("Aucune URL Captain Coaster valide n'a été trouvée pour cet import.");
        }

        return orderedUrls;
    }


    internal static async Task EnrichParkCoordinatesAsync(this CaptainCoasterDataSourceProvider provider, CaptainCoasterSyncSessionDocument session, CaptainCoasterScrapingSettings scrapingSettings, CancellationToken cancellationToken)
    {
        if (!scrapingSettings.EnrichParkCoordinates)
        {
            provider.AddLog(session, "Info", "Enrichissement des coordonnées désactivé pour cette exécution.");
            session.LastCompletedStep = "EnrichParkCoordinates";
            await provider.PersistSessionAsync(session, cancellationToken);
            return;
        }

        List<CaptainCoasterParkSnapshotDocument> parks = await provider.parksCollection
            .Find(item => item.SyncSessionId == session.Id)
            .ToListAsync(cancellationToken);

        string html = await provider.dataAcquisitionHttpFetcher.GetStringAsync(
            scrapingSettings.MapPageUrl,
            scrapingSettings.Language + ";q=1.0,en;q=0.8",
            provider.BuildRequestOptions(scrapingSettings),
            cancellationToken);

        IReadOnlyCollection<CaptainCoasterParkCoordinate> coordinates = provider.mapPageParser.Parse(scrapingSettings.MapPageUrl, html, scrapingSettings.MapMarkersAttributeName);
        Dictionary<string, CaptainCoasterParkCoordinate> coordinatesBySlug = coordinates
            .GroupBy(static item => item.Name.ToSlugValue(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (CaptainCoasterParkSnapshotDocument park in parks)
        {
            string slug = park.Name.ToSlugValue();
            if (coordinatesBySlug.TryGetValue(slug, out CaptainCoasterParkCoordinate? coordinate))
            {
                park.Latitude = coordinate.Latitude;
                park.Longitude = coordinate.Longitude;
                park.SourceUrl ??= coordinate.SourceUrl;
                park.RefreshLocation();
                park.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (parks.Count > 0)
        {
            List<WriteModel<CaptainCoasterParkSnapshotDocument>> operations = parks
                .Select(park =>
                {
                    park.RefreshLocation();
                    return (WriteModel<CaptainCoasterParkSnapshotDocument>)new ReplaceOneModel<CaptainCoasterParkSnapshotDocument>(
                        Builders<CaptainCoasterParkSnapshotDocument>.Filter.Eq(item => item.Id, park.Id),
                        park)
                    {
                        IsUpsert = false,
                    };
                })
                .ToList();

            await provider.parksCollection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
        }

        session.LastCompletedStep = "EnrichParkCoordinates";
        provider.AddLog(session, "Info", $"Coordonnées enrichies depuis la carte Captain Coaster pour {parks.Count(static item => item.Latitude.HasValue && item.Longitude.HasValue)} parc(s).");
        await provider.PersistSessionAsync(session, cancellationToken);
    }

    internal static async Task BuildComparisonFromStagingAsync(this CaptainCoasterDataSourceProvider provider, CaptainCoasterSyncSessionDocument session, CancellationToken cancellationToken)
    {
        List<CaptainCoasterParkSnapshotDocument> parks = await provider.parksCollection.Find(item => item.SyncSessionId == session.Id).ToListAsync(cancellationToken);
        List<CaptainCoasterCoasterSnapshotDocument> coasters = await provider.coastersCollection.Find(item => item.SyncSessionId == session.Id).ToListAsync(cancellationToken);
        List<CaptainCoasterComparisonResultDocument> comparisonResults = await provider.BuildComparisonResultsAsync(session.Id, parks, coasters, cancellationToken);
        await provider.comparisonCollection.DeleteManyAsync(item => item.SyncSessionId == session.Id, cancellationToken);
        if (comparisonResults.Count > 0)
        {
            await provider.comparisonCollection.InsertManyAsync(comparisonResults, cancellationToken: cancellationToken);
        }

        session.Metrics.ComparisonResults = comparisonResults.Count;
        session.Metrics.DuplicateConflicts = comparisonResults.Count(static item => item.RequiresManualResolution);
        session.LastCompletedStep = "BuildComparison";
        provider.AddLog(session, "Info", $"{comparisonResults.Count} différence(s) détectée(s), dont {session.Metrics.DuplicateConflicts} conflit(s) nécessitant une résolution humaine.");
        await provider.PersistSessionAsync(session, cancellationToken);
    }

    internal static CaptainCoasterCoasterSnapshotDocument MapParsedCoaster(this CaptainCoasterDataSourceProvider provider, string sessionId, CaptainCoasterParsedCoaster parsed)
    {
        return new CaptainCoasterCoasterSnapshotDocument
        {
            SourceKey = CaptainCoasterDataSourceProvider.SourceKeyValue,
            SyncSessionId = sessionId,
            CaptainCoasterId = parsed.ExternalId,
            Name = parsed.Name,
            Slug = parsed.Slug,
            SourceUrl = parsed.SourceUrl,
            ParkCaptainCoasterId = parsed.ParkSlug,
            ParkName = parsed.ParkName,
            CountryRaw = provider.NormalizeNullableText(parsed.CountryRaw),
            CountryCode = CountryNameMapper.ToCountryCode(parsed.CountryRaw),
            Manufacturer = parsed.Manufacturer,
            Model = parsed.Model,
            MaterialType = parsed.MaterialType,
            SeatingType = parsed.SeatingType,
            LaunchType = parsed.LaunchType,
            Restraint = parsed.RestraintType,
            IsLaunched = parsed.IsLaunched ?? false,
            HeightInMeters = parsed.HeightInMeters,
            LengthInMeters = parsed.LengthInMeters,
            SpeedInKmH = parsed.SpeedInKmH,
            DropInMeters = null,
            InversionCount = parsed.InversionCount,
            Status = parsed.Status,
            OpeningDate = PartialDateParser.Parse(parsed.OpeningDateText),
            ClosingDate = PartialDateParser.Parse(parsed.ClosingDateText),
            ScrapedAtUtc = parsed.ScrapedAtUtc,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    internal static List<CaptainCoasterParkSnapshotDocument> BuildParkSnapshots(this CaptainCoasterDataSourceProvider provider, string sessionId, IReadOnlyCollection<CaptainCoasterCoasterSnapshotDocument> coasters)
    {
        return coasters
            .Where(static item => !string.IsNullOrWhiteSpace(item.ParkName))
            .GroupBy(static item => item.ParkName!, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                string? countryRaw = provider.PickMostFrequentNonBlank(group.Select(static item => item.CountryRaw));
                string? countryCode = provider.PickMostFrequentNonBlank(group.Select(static item => item.CountryCode));
                if (string.IsNullOrWhiteSpace(countryCode))
                {
                    countryCode = CountryNameMapper.ToCountryCode(countryRaw);
                }

                CaptainCoasterParkSnapshotDocument document = new CaptainCoasterParkSnapshotDocument
                {
                    SourceKey = CaptainCoasterDataSourceProvider.SourceKeyValue,
                    SyncSessionId = sessionId,
                    CaptainCoasterId = group.First().ParkCaptainCoasterId ?? group.Key.ToSlugValue(),
                    Name = group.Key,
                    Slug = group.First().ParkCaptainCoasterId,
                    SourceUrl = group.First().SourceUrl,
                    CountryRaw = countryRaw,
                    CountryCode = provider.NormalizeCountryCodeForStorage(countryCode),
                    Latitude = null,
                    Longitude = null,
                    CoasterCount = group.Count(),
                    SampleCoasterNames = group.Select(static item => item.Name).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList(),
                    ScrapedAtUtc = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                document.RefreshLocation();
                return document;
            })
            .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static string? PickMostFrequentNonBlank(this CaptainCoasterDataSourceProvider provider, IEnumerable<string?> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim())
            .GroupBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(static group => group.Count())
            .ThenBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.Key)
            .FirstOrDefault();
    }

    internal static string? NormalizeNullableText(this CaptainCoasterDataSourceProvider provider, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    internal static CaptainCoasterScrapingSettings BuildScrapingSettings(this CaptainCoasterDataSourceProvider provider, DataSourceImportDescriptor importDescriptor, CaptainCoasterSettingsDocument settings)
    {
        int? maxCoasterCountOverride = provider.TryParseInt(provider.GetOption(importDescriptor.Options, "maxCoasterCount"));
        int? skipCountOverride = provider.TryParseInt(provider.GetOption(importDescriptor.Options, "skipCoasterCount"));
        int? delayOverride = provider.TryParseInt(provider.GetOption(importDescriptor.Options, "delayBetweenRequestsMs"));
        int? timeoutOverride = provider.TryParseInt(provider.GetOption(importDescriptor.Options, "httpTimeoutSeconds"));
        int? retryOverride = provider.TryParseInt(provider.GetOption(importDescriptor.Options, "maxRetryCount"));
        int? concurrentOverride = provider.TryParseInt(provider.GetOption(importDescriptor.Options, "maxConcurrentRequests"));
        int? writeBatchOverride = provider.TryParseInt(provider.GetOption(importDescriptor.Options, "coasterWriteBatchSize"));
        int? progressSaveOverride = provider.TryParseInt(provider.GetOption(importDescriptor.Options, "progressSaveInterval"));

        return new CaptainCoasterScrapingSettings
        {
            SitemapUrl = provider.GetOption(importDescriptor.Options, "sitemapUrl") ?? settings.SitemapUrl ?? "https://captaincoaster.com/sitemap.xml",
            MapPageUrl = provider.GetOption(importDescriptor.Options, "mapPageUrl") ?? settings.MapPageUrl ?? "https://captaincoaster.com/fr/map/",
            Language = provider.GetOption(importDescriptor.Options, "language") ?? "fr",
            DelayBetweenRequestsMs = Math.Max(0, delayOverride ?? settings.DelayBetweenRequestsMs),
            TimeoutSeconds = Math.Max(5, timeoutOverride ?? settings.HttpTimeoutSeconds),
            MaxRetryCount = Math.Max(1, retryOverride ?? settings.MaxRetryCount),
            MaxConcurrentRequests = Math.Clamp(concurrentOverride ?? settings.MaxConcurrentRequests, 1, 16),
            CoasterWriteBatchSize = Math.Clamp(writeBatchOverride ?? settings.CoasterWriteBatchSize, 5, 500),
            ProgressSaveInterval = Math.Clamp(progressSaveOverride ?? settings.ProgressSaveInterval, 1, 500),
            MaxCoasterCount = maxCoasterCountOverride ?? settings.MaxCoasterCount,
            SkipCoasterCount = Math.Max(0, skipCountOverride ?? settings.SkipCoasterCount),
            EnrichParkCoordinates = provider.GetOption(importDescriptor.Options, "enrichParkCoordinates") is string value ? provider.TryParseBool(value) : settings.EnrichParkCoordinates,
            MapMarkersAttributeName = provider.GetOption(importDescriptor.Options, "mapMarkersAttributeName") ?? settings.MapMarkersAttributeName,
            CoasterTitleXPath = provider.GetOption(importDescriptor.Options, "coasterTitleXPath") ?? settings.CoasterTitleXPath,
            CharacteristicsItemXPath = provider.GetOption(importDescriptor.Options, "characteristicsItemXPath") ?? settings.CharacteristicsItemXPath,
            CharacteristicLabelXPath = provider.GetOption(importDescriptor.Options, "characteristicLabelXPath") ?? settings.CharacteristicLabelXPath,
            CharacteristicValueXPath = provider.GetOption(importDescriptor.Options, "characteristicValueXPath") ?? settings.CharacteristicValueXPath,
            TopMetricXPath = provider.GetOption(importDescriptor.Options, "topMetricXPath") ?? settings.TopMetricXPath,
        };
    }

    internal static bool ShouldRunStep(this CaptainCoasterDataSourceProvider provider, string startAtStep, string candidateStep)
    {
        return provider.GetStepOrder(candidateStep) >= provider.GetStepOrder(startAtStep);
    }

    internal static string NormalizeStartStep(this CaptainCoasterDataSourceProvider provider, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "DiscoverUrls";
        }

        string trimmed = value.Trim();
        if (string.Equals(trimmed, "Coordinates", StringComparison.OrdinalIgnoreCase))
        {
            return "EnrichParkCoordinates";
        }

        if (string.Equals(trimmed, "RefreshSearchIndex", StringComparison.OrdinalIgnoreCase))
        {
            return "BuildComparison";
        }

        return trimmed;
    }

    internal static int GetStepOrder(this CaptainCoasterDataSourceProvider provider, string step)
    {
        if (string.Equals(step, "DiscoverUrls", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }
        if (string.Equals(step, "FetchCoasters", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }
        if (string.Equals(step, "EnrichParkCoordinates", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }
        if (string.Equals(step, "BuildComparison", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }
        return 0;
    }

    internal static int CalculateFetchProgress(this CaptainCoasterDataSourceProvider provider, int current, int total)
    {
        if (total <= 0)
        {
            return 70;
        }

        double ratio = Math.Clamp((double)current / total, 0d, 1d);
        return 15 + (int)Math.Round(ratio * 55d, MidpointRounding.AwayFromZero);
    }
}
