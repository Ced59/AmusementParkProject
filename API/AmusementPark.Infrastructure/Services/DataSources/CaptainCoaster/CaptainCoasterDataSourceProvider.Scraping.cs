using System.Globalization;
using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;

internal sealed partial class CaptainCoasterDataSourceProvider : IDataSourceProvider, IDataSourceImportExecutor
{
    private async Task ExecuteScrapingImportAsync(DataSourceImportJob job, CaptainCoasterSyncSessionDocument session, CancellationToken cancellationToken)
    {
        CaptainCoasterSettingsDocument settingsDocument = await this.GetOrCreateSettingsAsync();
        CaptainCoasterScrapingSettings scrapingSettings = BuildScrapingSettings(job.ImportDescriptor, settingsDocument);
        string startAtStep = NormalizeStartStep(GetOption(job.ImportDescriptor.Options, "startAtStep"));

        IReadOnlyCollection<CaptainCoasterDiscoveredUrl> discoveredUrls;
        if (ShouldRunStep(startAtStep, "DiscoverUrls"))
        {
            await this.UpdateSessionAsync(session, "DiscoverUrls", "Découverte des URLs à analyser.", 5, cancellationToken);
            discoveredUrls = await DiscoverUrlsAsync(job.ImportDescriptor, scrapingSettings, cancellationToken);
            await this.StageDiscoveredUrlsAsync(session.Id, discoveredUrls, cancellationToken);
            session.DiscoveredUrls = null;
            session.Metrics.DiscoveredItems = discoveredUrls.Count;
            session.LastCompletedStep = "DiscoverUrls";
            AddLog(session, "Info", $"{discoveredUrls.Count} URL(s) retenue(s) pour le traitement.");
            await this.PersistSessionAsync(session, cancellationToken);
        }
        else
        {
            discoveredUrls = await this.LoadDiscoveredUrlsAsync(session, scrapingSettings.Language, cancellationToken);

            if (discoveredUrls.Count == 0)
            {
                throw new InvalidOperationException("Aucune URL découverte n'est disponible pour reprendre le workflow depuis cette étape.");
            }
        }

        if (ShouldRunStep(startAtStep, "FetchCoasters"))
        {
            await this.UpdateSessionAsync(session, "FetchCoasters", "Téléchargement et parsing des pages coaster.", 15, cancellationToken);
            await ProcessCoasterPagesAsync(session, discoveredUrls, scrapingSettings, cancellationToken);
        }

        if (ShouldRunStep(startAtStep, "EnrichParkCoordinates"))
        {
            await this.UpdateSessionAsync(session, "EnrichParkCoordinates", "Enrichissement des coordonnées de parcs.", 75, cancellationToken);
            await EnrichParkCoordinatesAsync(session, scrapingSettings, cancellationToken);
        }

        if (ShouldRunStep(startAtStep, "BuildComparison"))
        {
            await this.UpdateSessionAsync(session, "BuildComparison", "Construction du rapport de comparaison.", 90, cancellationToken);
            await BuildComparisonFromStagingAsync(session, cancellationToken);
        }

        settingsDocument.LastSuccessfulSyncUtc = DateTime.UtcNow;
        settingsDocument.UpdatedAt = DateTime.UtcNow;
        await this.settingsCollection.ReplaceOneAsync(item => item.Id == settingsDocument.Id, settingsDocument, new ReplaceOptions { IsUpsert = true }, cancellationToken);

        session.Status = "Completed";
        session.CurrentStep = "Completed";
        session.Message = "Import Captain Coaster terminé. Les changements sont prêts pour validation manuelle.";
        session.ProgressPercentage = 100;
        session.CompletedAtUtc = DateTime.UtcNow;
        session.CanResume = true;
        session.UpdatedAt = DateTime.UtcNow;
        AddLog(session, "Info", $"Terminé : {session.Metrics.ParksFetched} parc(s), {session.Metrics.CoastersFetched} coaster(s), {session.Metrics.ComparisonResults} résultat(s). Les changements restent en attente de validation manuelle avant intégration métier.");
        await this.PersistSessionAsync(session, cancellationToken);
    }

    private async Task<IReadOnlyCollection<CaptainCoasterDiscoveredUrl>> DiscoverUrlsAsync(
        DataSourceImportDescriptor importDescriptor,
        CaptainCoasterScrapingSettings scrapingSettings,
        CancellationToken cancellationToken)
    {
        string importKind = NormalizeImportKind(importDescriptor.ImportKind);
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
            string sitemapContent = await this.dataAcquisitionHttpFetcher.GetStringAsync(
                scrapingSettings.SitemapUrl,
                scrapingSettings.Language + ";q=1.0,en;q=0.8",
                BuildRequestOptions(scrapingSettings),
                cancellationToken);

            IReadOnlyCollection<string> sitemapUrls = this.xmlSitemapUrlDiscoveryService.ReadUrls(sitemapContent);
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


    private async Task EnrichParkCoordinatesAsync(CaptainCoasterSyncSessionDocument session, CaptainCoasterScrapingSettings scrapingSettings, CancellationToken cancellationToken)
    {
        if (!scrapingSettings.EnrichParkCoordinates)
        {
            AddLog(session, "Info", "Enrichissement des coordonnées désactivé pour cette exécution.");
            session.LastCompletedStep = "EnrichParkCoordinates";
            await this.PersistSessionAsync(session, cancellationToken);
            return;
        }

        List<CaptainCoasterParkSnapshotDocument> parks = await this.parksCollection
            .Find(item => item.SyncSessionId == session.Id)
            .ToListAsync(cancellationToken);

        string html = await this.dataAcquisitionHttpFetcher.GetStringAsync(
            scrapingSettings.MapPageUrl,
            scrapingSettings.Language + ";q=1.0,en;q=0.8",
            BuildRequestOptions(scrapingSettings),
            cancellationToken);

        IReadOnlyCollection<CaptainCoasterParkCoordinate> coordinates = this.mapPageParser.Parse(scrapingSettings.MapPageUrl, html, scrapingSettings.MapMarkersAttributeName);
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

            await this.parksCollection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
        }

        session.LastCompletedStep = "EnrichParkCoordinates";
        AddLog(session, "Info", $"Coordonnées enrichies depuis la carte Captain Coaster pour {parks.Count(static item => item.Latitude.HasValue && item.Longitude.HasValue)} parc(s).");
        await this.PersistSessionAsync(session, cancellationToken);
    }

    private async Task BuildComparisonFromStagingAsync(CaptainCoasterSyncSessionDocument session, CancellationToken cancellationToken)
    {
        List<CaptainCoasterParkSnapshotDocument> parks = await this.parksCollection.Find(item => item.SyncSessionId == session.Id).ToListAsync(cancellationToken);
        List<CaptainCoasterCoasterSnapshotDocument> coasters = await this.coastersCollection.Find(item => item.SyncSessionId == session.Id).ToListAsync(cancellationToken);
        List<CaptainCoasterComparisonResultDocument> comparisonResults = await this.BuildComparisonResultsAsync(session.Id, parks, coasters, cancellationToken);
        await this.comparisonCollection.DeleteManyAsync(item => item.SyncSessionId == session.Id, cancellationToken);
        if (comparisonResults.Count > 0)
        {
            await this.comparisonCollection.InsertManyAsync(comparisonResults, cancellationToken: cancellationToken);
        }

        session.Metrics.ComparisonResults = comparisonResults.Count;
        session.Metrics.DuplicateConflicts = comparisonResults.Count(static item => item.RequiresManualResolution);
        session.LastCompletedStep = "BuildComparison";
        AddLog(session, "Info", $"{comparisonResults.Count} différence(s) détectée(s), dont {session.Metrics.DuplicateConflicts} conflit(s) nécessitant une résolution humaine.");
        await this.PersistSessionAsync(session, cancellationToken);
    }

    private static CaptainCoasterCoasterSnapshotDocument MapParsedCoaster(string sessionId, CaptainCoasterParsedCoaster parsed)
    {
        return new CaptainCoasterCoasterSnapshotDocument
        {
            SourceKey = SourceKeyValue,
            SyncSessionId = sessionId,
            CaptainCoasterId = parsed.ExternalId,
            Name = parsed.Name,
            Slug = parsed.Slug,
            SourceUrl = parsed.SourceUrl,
            ParkCaptainCoasterId = parsed.ParkSlug,
            ParkName = parsed.ParkName,
            CountryRaw = NormalizeNullableText(parsed.CountryRaw),
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

    private static List<CaptainCoasterParkSnapshotDocument> BuildParkSnapshots(string sessionId, IReadOnlyCollection<CaptainCoasterCoasterSnapshotDocument> coasters)
    {
        return coasters
            .Where(static item => !string.IsNullOrWhiteSpace(item.ParkName))
            .GroupBy(static item => item.ParkName!, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                string? countryRaw = PickMostFrequentNonBlank(group.Select(static item => item.CountryRaw));
                string? countryCode = PickMostFrequentNonBlank(group.Select(static item => item.CountryCode));
                if (string.IsNullOrWhiteSpace(countryCode))
                {
                    countryCode = CountryNameMapper.ToCountryCode(countryRaw);
                }

                CaptainCoasterParkSnapshotDocument document = new CaptainCoasterParkSnapshotDocument
                {
                    SourceKey = SourceKeyValue,
                    SyncSessionId = sessionId,
                    CaptainCoasterId = group.First().ParkCaptainCoasterId ?? group.Key.ToSlugValue(),
                    Name = group.Key,
                    Slug = group.First().ParkCaptainCoasterId,
                    SourceUrl = group.First().SourceUrl,
                    CountryRaw = countryRaw,
                    CountryCode = NormalizeCountryCodeForStorage(countryCode),
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

    private static string? PickMostFrequentNonBlank(IEnumerable<string?> values)
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

    private static string? NormalizeNullableText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static CaptainCoasterScrapingSettings BuildScrapingSettings(DataSourceImportDescriptor importDescriptor, CaptainCoasterSettingsDocument settings)
    {
        int? maxCoasterCountOverride = TryParseInt(GetOption(importDescriptor.Options, "maxCoasterCount"));
        int? skipCountOverride = TryParseInt(GetOption(importDescriptor.Options, "skipCoasterCount"));
        int? delayOverride = TryParseInt(GetOption(importDescriptor.Options, "delayBetweenRequestsMs"));
        int? timeoutOverride = TryParseInt(GetOption(importDescriptor.Options, "httpTimeoutSeconds"));
        int? retryOverride = TryParseInt(GetOption(importDescriptor.Options, "maxRetryCount"));
        int? concurrentOverride = TryParseInt(GetOption(importDescriptor.Options, "maxConcurrentRequests"));
        int? writeBatchOverride = TryParseInt(GetOption(importDescriptor.Options, "coasterWriteBatchSize"));
        int? progressSaveOverride = TryParseInt(GetOption(importDescriptor.Options, "progressSaveInterval"));

        return new CaptainCoasterScrapingSettings
        {
            SitemapUrl = GetOption(importDescriptor.Options, "sitemapUrl") ?? settings.SitemapUrl ?? "https://captaincoaster.com/sitemap.xml",
            MapPageUrl = GetOption(importDescriptor.Options, "mapPageUrl") ?? settings.MapPageUrl ?? "https://captaincoaster.com/fr/map/",
            Language = GetOption(importDescriptor.Options, "language") ?? "fr",
            DelayBetweenRequestsMs = Math.Max(0, delayOverride ?? settings.DelayBetweenRequestsMs),
            TimeoutSeconds = Math.Max(5, timeoutOverride ?? settings.HttpTimeoutSeconds),
            MaxRetryCount = Math.Max(1, retryOverride ?? settings.MaxRetryCount),
            MaxConcurrentRequests = Math.Clamp(concurrentOverride ?? settings.MaxConcurrentRequests, 1, 16),
            CoasterWriteBatchSize = Math.Clamp(writeBatchOverride ?? settings.CoasterWriteBatchSize, 5, 500),
            ProgressSaveInterval = Math.Clamp(progressSaveOverride ?? settings.ProgressSaveInterval, 1, 500),
            MaxCoasterCount = maxCoasterCountOverride ?? settings.MaxCoasterCount,
            SkipCoasterCount = Math.Max(0, skipCountOverride ?? settings.SkipCoasterCount),
            EnrichParkCoordinates = GetOption(importDescriptor.Options, "enrichParkCoordinates") is string value ? TryParseBool(value) : settings.EnrichParkCoordinates,
            MapMarkersAttributeName = GetOption(importDescriptor.Options, "mapMarkersAttributeName") ?? settings.MapMarkersAttributeName,
            CoasterTitleXPath = GetOption(importDescriptor.Options, "coasterTitleXPath") ?? settings.CoasterTitleXPath,
            CharacteristicsItemXPath = GetOption(importDescriptor.Options, "characteristicsItemXPath") ?? settings.CharacteristicsItemXPath,
            CharacteristicLabelXPath = GetOption(importDescriptor.Options, "characteristicLabelXPath") ?? settings.CharacteristicLabelXPath,
            CharacteristicValueXPath = GetOption(importDescriptor.Options, "characteristicValueXPath") ?? settings.CharacteristicValueXPath,
            TopMetricXPath = GetOption(importDescriptor.Options, "topMetricXPath") ?? settings.TopMetricXPath,
        };
    }

    private static bool ShouldRunStep(string startAtStep, string candidateStep)
    {
        return GetStepOrder(candidateStep) >= GetStepOrder(startAtStep);
    }

    private static string NormalizeStartStep(string? value)
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

    private static int GetStepOrder(string step)
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

    private static int CalculateFetchProgress(int current, int total)
    {
        if (total <= 0)
        {
            return 70;
        }

        double ratio = Math.Clamp((double)current / total, 0d, 1d);
        return 15 + (int)Math.Round(ratio * 55d, MidpointRounding.AwayFromZero);
    }
}
