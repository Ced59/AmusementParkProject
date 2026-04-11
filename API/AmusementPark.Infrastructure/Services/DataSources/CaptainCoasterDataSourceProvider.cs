using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Results;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Services.DataSources.Acquisition;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoasterScraping;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Services.DataSources;

internal sealed partial class CaptainCoasterDataSourceProvider : IDataSourceProvider, IDataSourceImportExecutor
{
    private const string SourceKeyValue = "captain-coaster";
    private const string DisplayNameValue = "Captain Coaster";
    private const string LegacyExternalSourceValue = "CaptainCoaster";

    private readonly IMongoCollection<CaptainCoasterSettingsDocument> settingsCollection;
    private readonly IMongoCollection<CaptainCoasterParkSnapshotDocument> parksCollection;
    private readonly IMongoCollection<CaptainCoasterCoasterSnapshotDocument> coastersCollection;
    private readonly IMongoCollection<CaptainCoasterDiscoveredUrlDocument> discoveredUrlsCollection;
    private readonly IMongoCollection<CaptainCoasterSyncSessionDocument> sessionsCollection;
    private readonly IMongoCollection<CaptainCoasterComparisonResultDocument> comparisonCollection;
    private readonly IMongoCollection<ParkDocument> localParksCollection;
    private readonly IMongoCollection<ParkItemDocument> localParkItemsCollection;
    private readonly IMongoCollection<AttractionManufacturerDocument> manufacturersCollection;
    private readonly IDataSourceImportJobQueue queue;
    private readonly IDataAcquisitionHttpFetcher dataAcquisitionHttpFetcher;
    private readonly IXmlSitemapUrlDiscoveryService xmlSitemapUrlDiscoveryService;
    private readonly ICaptainCoasterCoasterPageParser coasterPageParser;
    private readonly ICaptainCoasterMapPageParser mapPageParser;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public CaptainCoasterDataSourceProvider(
        IMongoDatabase database,
        MongoDbSettings mongoDbSettings,
        IDataSourceImportJobQueue queue,
        IDataAcquisitionHttpFetcher dataAcquisitionHttpFetcher,
        IXmlSitemapUrlDiscoveryService xmlSitemapUrlDiscoveryService,
        ICaptainCoasterCoasterPageParser coasterPageParser,
        ICaptainCoasterMapPageParser mapPageParser,
        ISearchProjectionWriter searchProjectionWriter)
    {
        this.settingsCollection = database.GetCollection<CaptainCoasterSettingsDocument>(mongoDbSettings.CaptainCoasterSettingsCollectionName);
        this.parksCollection = database.GetCollection<CaptainCoasterParkSnapshotDocument>(mongoDbSettings.CaptainCoasterParksCollectionName);
        this.coastersCollection = database.GetCollection<CaptainCoasterCoasterSnapshotDocument>(mongoDbSettings.CaptainCoasterCoastersCollectionName);
        this.discoveredUrlsCollection = database.GetCollection<CaptainCoasterDiscoveredUrlDocument>(mongoDbSettings.CaptainCoasterDiscoveredUrlsCollectionName);
        this.sessionsCollection = database.GetCollection<CaptainCoasterSyncSessionDocument>(mongoDbSettings.CaptainCoasterSyncSessionsCollectionName);
        this.comparisonCollection = database.GetCollection<CaptainCoasterComparisonResultDocument>(mongoDbSettings.CaptainCoasterComparisonResultsCollectionName);
        this.localParksCollection = database.GetCollection<ParkDocument>(mongoDbSettings.ParksCollectionName);
        this.localParkItemsCollection = database.GetCollection<ParkItemDocument>(mongoDbSettings.ParkItemsCollectionName);
        this.manufacturersCollection = database.GetCollection<AttractionManufacturerDocument>(mongoDbSettings.AttractionManufacturersCollectionName);
        this.queue = queue;
        this.dataAcquisitionHttpFetcher = dataAcquisitionHttpFetcher;
        this.xmlSitemapUrlDiscoveryService = xmlSitemapUrlDiscoveryService;
        this.coasterPageParser = coasterPageParser;
        this.mapPageParser = mapPageParser;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public string SourceKey => SourceKeyValue;

    public async Task<DataSourceStatusResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        CaptainCoasterSettingsDocument settings = await this.GetOrCreateSettingsAsync();
        FilterDefinition<CaptainCoasterSyncSessionDocument> filter = Builders<CaptainCoasterSyncSessionDocument>.Filter.Eq(item => item.SourceKey, SourceKeyValue);
        long totalSessions = await this.sessionsCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        return new DataSourceStatusResult
        {
            SourceKey = SourceKeyValue,
            DisplayName = DisplayNameValue,
            IsEnabled = settings.IsEnabled,
            LastSuccessfulImportUtc = settings.LastSuccessfulSyncUtc,
            TotalSessionsCount = (int)totalSessions,
        };
    }

    public async Task<DataSourceSettingsResult> GetSettingsAsync(CancellationToken cancellationToken)
    {
        CaptainCoasterSettingsDocument settings = await this.GetOrCreateSettingsAsync();
        return MapSettings(settings);
    }

    public async Task<DataSourceSettingsResult> UpdateSettingsAsync(DataSourceSettingsResult settings, CancellationToken cancellationToken)
    {
        CaptainCoasterSettingsDocument document = await this.GetOrCreateSettingsAsync();
        document.IsEnabled = settings.IsEnabled;
        document.DataDirectoryPath = GetOption(settings.Options, "dataDirectoryPath");
        document.HtmlDirectoryPath = GetOption(settings.Options, "htmlDirectoryPath");
        document.UseOfflineMode = TryParseBool(GetOption(settings.Options, "useOfflineMode"));

        string? baseUrl = GetOption(settings.Options, "baseUrl");
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            document.BaseUrl = baseUrl.Trim();
        }

        string? apiKey = GetOption(settings.Options, "apiKey");
        if (apiKey != null)
        {
            document.ApiKey = apiKey;
        }

        document.SitemapUrl = GetOption(settings.Options, "sitemapUrl") ?? document.SitemapUrl ?? "https://captaincoaster.com/sitemap.xml";
        document.MapPageUrl = GetOption(settings.Options, "mapPageUrl") ?? document.MapPageUrl ?? "https://captaincoaster.com/fr/map/";
        document.DelayBetweenRequestsMs = Math.Max(0, TryParseInt(GetOption(settings.Options, "delayBetweenRequestsMs")) ?? document.DelayBetweenRequestsMs);
        document.HttpTimeoutSeconds = Math.Max(5, TryParseInt(GetOption(settings.Options, "httpTimeoutSeconds")) ?? document.HttpTimeoutSeconds);
        document.MaxRetryCount = Math.Max(1, TryParseInt(GetOption(settings.Options, "maxRetryCount")) ?? document.MaxRetryCount);
        document.MaxConcurrentRequests = Math.Clamp(TryParseInt(GetOption(settings.Options, "maxConcurrentRequests")) ?? document.MaxConcurrentRequests, 1, 16);
        document.CoasterWriteBatchSize = Math.Clamp(TryParseInt(GetOption(settings.Options, "coasterWriteBatchSize")) ?? document.CoasterWriteBatchSize, 5, 500);
        document.ProgressSaveInterval = Math.Clamp(TryParseInt(GetOption(settings.Options, "progressSaveInterval")) ?? document.ProgressSaveInterval, 1, 500);
        document.MaxCoasterCount = TryParseInt(GetOption(settings.Options, "maxCoasterCount")) ?? document.MaxCoasterCount;
        document.SkipCoasterCount = Math.Max(0, TryParseInt(GetOption(settings.Options, "skipCoasterCount")) ?? document.SkipCoasterCount);
        document.EnrichParkCoordinates = GetOption(settings.Options, "enrichParkCoordinates") is string enrichValue ? TryParseBool(enrichValue) : document.EnrichParkCoordinates;
        document.MapMarkersAttributeName = GetOption(settings.Options, "mapMarkersAttributeName") ?? document.MapMarkersAttributeName;
        document.CoasterTitleXPath = GetOption(settings.Options, "coasterTitleXPath") ?? document.CoasterTitleXPath;
        document.CharacteristicsItemXPath = GetOption(settings.Options, "characteristicsItemXPath") ?? document.CharacteristicsItemXPath;
        document.CharacteristicLabelXPath = GetOption(settings.Options, "characteristicLabelXPath") ?? document.CharacteristicLabelXPath;
        document.CharacteristicValueXPath = GetOption(settings.Options, "characteristicValueXPath") ?? document.CharacteristicValueXPath;
        document.TopMetricXPath = GetOption(settings.Options, "topMetricXPath") ?? document.TopMetricXPath;

        document.Source = LegacyExternalSourceValue;
        document.UpdatedAt = DateTime.UtcNow;
        ReplaceOptions options = new ReplaceOptions { IsUpsert = true };
        await this.settingsCollection.ReplaceOneAsync(item => item.Id == document.Id, document, options, cancellationToken);
        return MapSettings(document);
    }

    public async Task<DataSourceSessionResult?> GetLatestSessionAsync(CancellationToken cancellationToken)
    {
        CaptainCoasterSyncSessionDocument? session = await this.sessionsCollection
            .Find(item => item.SourceKey == SourceKeyValue)
            .SortByDescending(item => item.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return session == null ? null : MapSession(session);
    }

    public async Task<DataSourceSessionResult?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken)
    {
        CaptainCoasterSyncSessionDocument? session = await this.sessionsCollection
            .Find(item => item.Id == sessionId && item.SourceKey == SourceKeyValue)
            .FirstOrDefaultAsync(cancellationToken);

        return session == null ? null : MapSession(session);
    }

    public async Task<DataSourceComparisonPageResult> GetComparisonResultsAsync(
        string? sessionId,
        string? entityType,
        string? changeType,
        bool? isApplied,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        int effectivePageSize = Math.Clamp(pageSize, 10, 200);
        int effectivePage = Math.Max(0, page);

        string? effectiveSessionId = sessionId;
        if (string.IsNullOrWhiteSpace(effectiveSessionId))
        {
            CaptainCoasterSyncSessionDocument? latest = await this.sessionsCollection
                .Find(item => item.SourceKey == SourceKeyValue)
                .SortByDescending(item => item.StartedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            effectiveSessionId = latest?.Id;
        }

        if (string.IsNullOrWhiteSpace(effectiveSessionId))
        {
            return new DataSourceComparisonPageResult
            {
                Page = effectivePage,
                PageSize = effectivePageSize,
            };
        }

        FilterDefinition<CaptainCoasterComparisonResultDocument> sessionFilter =
            Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.SyncSessionId, effectiveSessionId)
            & Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.SourceKey, SourceKeyValue);

        Task<long> updatedTask = this.comparisonCollection.CountDocumentsAsync(
            sessionFilter & Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.ChangeType, "Updated"),
            cancellationToken: cancellationToken);
        Task<long> missingTask = this.comparisonCollection.CountDocumentsAsync(
            sessionFilter & Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.ChangeType, "MissingLocal"),
            cancellationToken: cancellationToken);
        Task<long> duplicateTask = this.comparisonCollection.CountDocumentsAsync(
            sessionFilter & Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.ChangeType, "DuplicateExternal"),
            cancellationToken: cancellationToken);
        Task<long> appliedTask = this.comparisonCollection.CountDocumentsAsync(
            sessionFilter & Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.IsApplied, true),
            cancellationToken: cancellationToken);

        FilterDefinition<CaptainCoasterComparisonResultDocument> pagedFilter = sessionFilter;
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            pagedFilter &= Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.EntityType, entityType);
        }
        if (!string.IsNullOrWhiteSpace(changeType))
        {
            pagedFilter &= Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.ChangeType, changeType);
        }
        if (isApplied.HasValue)
        {
            pagedFilter &= Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.IsApplied, isApplied.Value);
        }

        Task<long> totalTask = this.comparisonCollection.CountDocumentsAsync(pagedFilter, cancellationToken: cancellationToken);
        await Task.WhenAll(updatedTask, missingTask, duplicateTask, appliedTask, totalTask);

        List<CaptainCoasterComparisonResultDocument> items = await this.comparisonCollection
            .Find(pagedFilter)
            .SortBy(item => item.EntityType)
            .ThenBy(item => item.ChangeType)
            .ThenBy(item => item.DisplayName)
            .Skip(effectivePage * effectivePageSize)
            .Limit(effectivePageSize)
            .ToListAsync(cancellationToken);

        return new DataSourceComparisonPageResult
        {
            Items = items.Select(MapComparison).ToList(),
            TotalCount = (int)totalTask.Result,
            Page = effectivePage,
            PageSize = effectivePageSize,
            SessionUpdatedCount = (int)updatedTask.Result,
            SessionMissingCount = (int)missingTask.Result,
            SessionDuplicateCount = (int)duplicateTask.Result,
            SessionAppliedCount = (int)appliedTask.Result,
        };
    }

    public async Task<DataSourceSessionResult> StartImportAsync(DataSourceImportDescriptor importDescriptor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(importDescriptor);

        string importKind = NormalizeImportKind(importDescriptor.ImportKind);
        if (!IsSupportedImportKind(importKind))
        {
            throw new ArgumentException($"Le mode d'import '{importKind}' n'est pas supporté.", nameof(importDescriptor));
        }

        CaptainCoasterSettingsDocument settings = await this.GetOrCreateSettingsAsync();
        if (!settings.IsEnabled)
        {
            throw new InvalidOperationException("La source Captain Coaster est désactivée.");
        }

        CaptainCoasterSyncSessionDocument? session = null;
        if (!string.IsNullOrWhiteSpace(importDescriptor.ResumeSessionId))
        {
            session = await this.sessionsCollection
                .Find(item => item.Id == importDescriptor.ResumeSessionId && item.SourceKey == SourceKeyValue)
                .FirstOrDefaultAsync(cancellationToken);

            if (session == null)
            {
                throw new ArgumentException("La session à reprendre est introuvable.", nameof(importDescriptor));
            }

            session.Status = "Pending";
            session.CurrentStep = "Queued";
            session.Message = "Reprise du workflow planifiée.";
            session.ProgressPercentage = 0;
            session.CompletedAtUtc = null;
            session.ImportKind = importKind;
            session.CanResume = true;
            session.AvailableSteps = GetAvailableSteps(importKind).ToList();
            session.UpdatedAt = DateTime.UtcNow;
            AddLog(session, "Info", "Reprise du workflow planifiée.");
            await this.PersistSessionAsync(session, cancellationToken);
        }
        else
        {
            FilterDefinition<CaptainCoasterSyncSessionDocument> runningFilter =
                Builders<CaptainCoasterSyncSessionDocument>.Filter.Eq(item => item.SourceKey, SourceKeyValue)
                & Builders<CaptainCoasterSyncSessionDocument>.Filter.Eq(item => item.CompletedAtUtc, null);
            long runningCount = await this.sessionsCollection.CountDocumentsAsync(runningFilter, cancellationToken: cancellationToken);
            if (runningCount > 0)
            {
                throw new InvalidOperationException("Un import Captain Coaster est déjà en cours.");
            }

            session = new CaptainCoasterSyncSessionDocument
            {
                SourceKey = SourceKeyValue,
                Status = "Pending",
                CurrentStep = "Queued",
                Message = "Import mis en file d'attente.",
                ProgressPercentage = 0,
                ImportKind = importKind,
                AvailableSteps = GetAvailableSteps(importKind).ToList(),
                CanResume = true,
                StartedAtUtc = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            AddLog(session, "Info", $"Import Captain Coaster planifié en mode '{importKind}'.");
            await this.sessionsCollection.InsertOneAsync(session, cancellationToken: cancellationToken);
        }

        await this.queue.EnqueueAsync(new DataSourceImportJob(SourceKeyValue, session.Id, importDescriptor), cancellationToken);
        return MapSession(session);
    }

    public async Task<DataSourceApplyResult> ApplyComparisonAsync(DataSourceApplyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? effectiveSessionId = request.SessionId;
        if (string.IsNullOrWhiteSpace(effectiveSessionId))
        {
            CaptainCoasterSyncSessionDocument? latestSession = await this.sessionsCollection
                .Find(item => item.SourceKey == SourceKeyValue)
                .SortByDescending(item => item.StartedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            effectiveSessionId = latestSession?.Id;
        }

        if (string.IsNullOrWhiteSpace(effectiveSessionId))
        {
            return new DataSourceApplyResult { AppliedCount = 0 };
        }

        CaptainCoasterSyncSessionDocument? session = await this.sessionsCollection
            .Find(item => item.Id == effectiveSessionId && item.SourceKey == SourceKeyValue)
            .FirstOrDefaultAsync(cancellationToken);

        if (session == null)
        {
            return new DataSourceApplyResult { AppliedCount = 0 };
        }

        List<CaptainCoasterComparisonResultDocument> results;
        if (request.ApplyAll)
        {
            FilterDefinition<CaptainCoasterComparisonResultDocument> filter =
                Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.SyncSessionId, effectiveSessionId)
                & Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.SourceKey, SourceKeyValue)
                & Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.IsApplied, false)
                & Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.RequiresManualResolution, false);

            if (!string.IsNullOrWhiteSpace(request.EntityTypeFilter))
            {
                filter &= Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.EntityType, request.EntityTypeFilter);
            }
            if (!string.IsNullOrWhiteSpace(request.ChangeTypeFilter))
            {
                filter &= Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.ChangeType, request.ChangeTypeFilter);
            }

            results = await this.comparisonCollection.Find(filter).ToListAsync(cancellationToken);
        }
        else
        {
            List<string> ids = request.ComparisonResultIds
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (ids.Count == 0)
            {
                return new DataSourceApplyResult { AppliedCount = 0 };
            }

            results = await this.comparisonCollection
                .Find(item => item.SourceKey == SourceKeyValue && item.SyncSessionId == effectiveSessionId && ids.Contains(item.Id))
                .ToListAsync(cancellationToken);
        }

        if (results.Count == 0)
        {
            return new DataSourceApplyResult { AppliedCount = 0 };
        }

        Dictionary<string, DataSourceDuplicateResolution> resolutionsByResultId = request.DuplicateResolutions
            .Where(item => !string.IsNullOrWhiteSpace(item.ComparisonResultId))
            .GroupBy(item => item.ComparisonResultId.Trim(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);

        List<CaptainCoasterComparisonResultDocument> orderedResults = results
            .OrderBy(item => GetEntityApplyPriority(item.EntityType))
            .ThenBy(item => item.RequiresManualResolution ? 1 : 0)
            .ThenBy(item => item.DisplayName)
            .ToList();

        CaptainCoasterSettingsDocument settings = await this.GetOrCreateSettingsAsync();
        int batchSize = NormalizePositiveBounded(settings.CoasterWriteBatchSize, 100, 10, 500);
        int progressSaveInterval = NormalizePositiveBounded(settings.ProgressSaveInterval, 25, 1, 500);
        CaptainCoasterApplyExecutionContext context = await this.BuildApplyExecutionContextAsync(session.Id, cancellationToken);

        int startingAppliedChanges = session.Metrics.AppliedChanges;
        int appliedCount = 0;
        int failedCount = 0;
        int processedCount = 0;
        int totalCount = orderedResults.Count;

        session.Status = "Applying";
        session.CurrentStep = "ApplyComparison";
        session.Message = $"Application métier en cours : 0/{totalCount} élément(s) traité(s).";
        session.ProgressPercentage = 0;
        session.CanResume = false;
        session.CompletedAtUtc = null;
        session.UpdatedAt = DateTime.UtcNow;
        AddLog(session, "Info", $"Application métier démarrée : {totalCount} changement(s) à traiter.");
        await this.PersistSessionAsync(session, cancellationToken);

        try
        {
            foreach (CaptainCoasterComparisonResultDocument result in orderedResults)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processedCount++;

                try
                {
                    resolutionsByResultId.TryGetValue(result.Id, out DataSourceDuplicateResolution? resolution);
                    DateTime utcNow = DateTime.UtcNow;
                    CaptainCoasterApplyImpact impact = new CaptainCoasterApplyImpact { Applied = false };
                    if (string.Equals(result.EntityType, "Park", StringComparison.OrdinalIgnoreCase))
                    {
                        impact = this.ApplyParkResultWithContext(result, resolution, context, utcNow);
                    }
                    else if (string.Equals(result.EntityType, "Coaster", StringComparison.OrdinalIgnoreCase))
                    {
                        impact = this.ApplyCoasterResultWithContext(result, resolution, context, utcNow);
                    }

                    if (impact.Applied)
                    {
                        appliedCount++;
                    }
                }
                catch (Exception exception)
                {
                    failedCount++;
                    AddLog(session, "Warn", $"Échec de l'application pour '{result.DisplayName}' : {exception.Message}");
                }

                if (HasPendingApplyWrites(context, batchSize))
                {
                    await this.FlushApplyWritesAsync(context, cancellationToken);
                }

                if (processedCount % progressSaveInterval == 0 || processedCount == totalCount)
                {
                    await this.FlushApplyWritesAsync(context, cancellationToken);
                    session.Metrics.AppliedChanges = startingAppliedChanges + appliedCount;
                    session.ProgressPercentage = (int)Math.Round((double)processedCount * 100d / Math.Max(1, totalCount));
                    session.Message = $"Application métier en cours : {processedCount}/{totalCount} élément(s) traité(s), {appliedCount} appliqué(s), {failedCount} en échec.";
                    session.UpdatedAt = DateTime.UtcNow;
                    AddLog(session, "Info", session.Message);
                    await this.PersistSessionAsync(session, cancellationToken);
                }
            }

            await this.FlushApplyWritesAsync(context, cancellationToken);

            session.CurrentStep = "RefreshSearchIndex";
            session.Message = "Rafraîchissement de l'index de recherche après application métier.";
            session.ProgressPercentage = 99;
            session.UpdatedAt = DateTime.UtcNow;
            AddLog(session, "Info", session.Message);
            await this.PersistSessionAsync(session, cancellationToken);

            await this.RefreshSearchProjectionAsync(
                session,
                context.AffectedParkIds.ToList(),
                context.AffectedParkItemIds.ToList(),
                cancellationToken);

            session.Metrics.AppliedChanges = startingAppliedChanges + appliedCount;
            session.Status = "Completed";
            session.CurrentStep = "Completed";
            session.LastCompletedStep = "ApplyComparison";
            session.Message = $"Application métier terminée : {appliedCount}/{totalCount} changement(s) appliqué(s), {failedCount} en échec.";
            session.ProgressPercentage = 100;
            session.CompletedAtUtc = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.CanResume = true;
            AddLog(session, "Info", session.Message);
            await this.PersistSessionAsync(session, cancellationToken);

            return new DataSourceApplyResult { AppliedCount = appliedCount };
        }
        catch (Exception exception)
        {
            session.Status = "Failed";
            session.CurrentStep = "ApplyComparison";
            session.Message = $"Échec de l'application métier : {exception.Message}";
            session.ProgressPercentage = Math.Min(session.ProgressPercentage, 99);
            session.CompletedAtUtc = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.CanResume = true;
            session.Metrics.AppliedChanges = startingAppliedChanges + appliedCount;
            AddLog(session, "Error", exception.ToString());
            await this.PersistSessionAsync(session, cancellationToken);
            throw;
        }
    }

    public async Task ExecuteImportAsync(DataSourceImportJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        string importKind = NormalizeImportKind(job.ImportDescriptor.ImportKind);
        CaptainCoasterSyncSessionDocument session = await this.sessionsCollection.Find(item => item.Id == job.SessionId).FirstAsync(cancellationToken);

        try
        {
            session.ImportKind = importKind;
            session.AvailableSteps = GetAvailableSteps(importKind).ToList();
            session.CanResume = true;
            await this.PersistSessionAsync(session, cancellationToken);

            if (!string.Equals(importKind, "json-files", StringComparison.OrdinalIgnoreCase))
            {
                await this.ExecuteScrapingImportAsync(job, session, cancellationToken);
                return;
            }

            CaptainCoasterImportFiles inputFiles = ResolveInputFiles(job.ImportDescriptor);
            byte[] parksBytes = await File.ReadAllBytesAsync(inputFiles.ParksFilePath, cancellationToken);
            byte[] coastersBytes = await File.ReadAllBytesAsync(inputFiles.CoastersFilePath, cancellationToken);

            await this.UpdateSessionAsync(session, "ParsingParks", "Analyse du fichier detected-parks.json.", 10, cancellationToken);
            List<CaptainCoasterParkSnapshotDocument> parks = ParseParksFromJson(job.SessionId, parksBytes);
            await this.parksCollection.DeleteManyAsync(item => item.SyncSessionId == job.SessionId, cancellationToken);
            if (parks.Count > 0)
            {
                await this.parksCollection.InsertManyAsync(parks, cancellationToken: cancellationToken);
            }
            session.Metrics.ParksFetched = parks.Count;
            int parkDuplicateGroups = CountDuplicateGroups(parks.Select(item => item.CaptainCoasterId));
            if (parkDuplicateGroups > 0)
            {
                AddLog(session, "Warn", $"{parkDuplicateGroups} doublon(s) d'identifiant parc détecté(s) dans le staging. Une résolution humaine sera demandée.");
            }
            session.LastCompletedStep = "ParsingParks";
            AddLog(session, "Info", $"{parks.Count} parc(s) parsé(s).");
            await this.PersistSessionAsync(session, cancellationToken);

            await this.UpdateSessionAsync(session, "ParsingCoasters", "Analyse du fichier coasters.json.", 40, cancellationToken);
            List<CaptainCoasterCoasterSnapshotDocument> coasters = ParseCoastersFromJson(job.SessionId, coastersBytes);
            await this.coastersCollection.DeleteManyAsync(item => item.SyncSessionId == job.SessionId, cancellationToken);
            if (coasters.Count > 0)
            {
                await this.coastersCollection.InsertManyAsync(coasters, cancellationToken: cancellationToken);
            }
            session.Metrics.CoastersFetched = coasters.Count;
            int coasterDuplicateGroups = CountDuplicateGroups(coasters.Select(item => item.CaptainCoasterId));
            if (coasterDuplicateGroups > 0)
            {
                AddLog(session, "Warn", $"{coasterDuplicateGroups} doublon(s) d'identifiant coaster détecté(s) dans le staging. Une résolution humaine sera demandée.");
            }
            session.LastCompletedStep = "ParsingCoasters";
            AddLog(session, "Info", $"{coasters.Count} coaster(s) parsé(s).");
            await this.PersistSessionAsync(session, cancellationToken);

            await this.UpdateSessionAsync(session, "BuildingComparison", "Construction du rapport de comparaison.", 70, cancellationToken);
            List<CaptainCoasterComparisonResultDocument> comparisonResults = await this.BuildComparisonResultsAsync(job.SessionId, parks, coasters, cancellationToken);
            await this.comparisonCollection.DeleteManyAsync(item => item.SyncSessionId == job.SessionId, cancellationToken);
            if (comparisonResults.Count > 0)
            {
                await this.comparisonCollection.InsertManyAsync(comparisonResults, cancellationToken: cancellationToken);
            }
            session.Metrics.ComparisonResults = comparisonResults.Count;
            session.Metrics.DuplicateConflicts = comparisonResults.Count(item => item.RequiresManualResolution);
            session.LastCompletedStep = "BuildComparison";
            AddLog(session, "Info", $"{comparisonResults.Count} différence(s) détectée(s), dont {session.Metrics.DuplicateConflicts} conflit(s) nécessitant une résolution humaine.");
            await this.PersistSessionAsync(session, cancellationToken);

            CaptainCoasterSettingsDocument settings = await this.GetOrCreateSettingsAsync();
            settings.LastSuccessfulSyncUtc = DateTime.UtcNow;
            settings.UpdatedAt = DateTime.UtcNow;
            await this.settingsCollection.ReplaceOneAsync(item => item.Id == settings.Id, settings, new ReplaceOptions { IsUpsert = true }, cancellationToken);

            session.Status = "Completed";
            session.CurrentStep = "Completed";
            session.Message = "Import Captain Coaster terminé. Les changements sont prêts pour validation manuelle.";
            session.ProgressPercentage = 100;
            session.CompletedAtUtc = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.CanResume = true;
            AddLog(session, "Info", $"Terminé : {session.Metrics.ParksFetched} parcs, {session.Metrics.CoastersFetched} coasters, {session.Metrics.ComparisonResults} résultats. Les changements restent en attente de validation manuelle avant intégration métier.");
            await this.PersistSessionAsync(session, cancellationToken);
        }
        catch (Exception exception)
        {
            session.Status = "Failed";
            session.CurrentStep = "Failed";
            session.Message = exception.Message;
            session.CompletedAtUtc = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.CanResume = true;
            AddLog(session, "Error", exception.ToString());
            await this.PersistSessionAsync(session, cancellationToken);
        }
        finally
        {
            DeleteWorkingDirectorySafe(job.ImportDescriptor.WorkingDirectoryPath);
        }
    }
}
