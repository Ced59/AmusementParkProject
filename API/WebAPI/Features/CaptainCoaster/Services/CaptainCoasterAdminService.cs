using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Entities.Model.Parks;
using MongoDB.Driver;
using Repositories.Interfaces;
using WebAPI.Features.CaptainCoaster.Contracts;
using WebAPI.Features.CaptainCoaster.Models;
using WebAPI.Settings.MongoDB;

namespace WebAPI.Features.CaptainCoaster.Services
{
    public interface ICaptainCoasterAdminService
    {
        Task<CaptainCoasterDataSourceStatusResponse> GetStatusAsync();
        Task<CaptainCoasterSyncSessionResponse?> GetLatestSessionAsync();
        Task<CaptainCoasterComparisonPagedResponse> GetComparisonResultsAsync(
            string? sessionId,
            string? entityType,
            string? changeType,
            bool? isApplied,
            int page,
            int pageSize);
        Task<CaptainCoasterSyncSessionResponse> StartImportFromFilesAsync(Stream parksStream, Stream coastersStream, CancellationToken cancellationToken);
        Task<int> ApplyComparisonResultsAsync(ApplyCaptainCoasterComparisonRequest request, CancellationToken cancellationToken);
    }

    public sealed class CaptainCoasterAdminService : ICaptainCoasterAdminService
    {
        private readonly IMongoCollection<CaptainCoasterDataSourceSettings> settingsCollection;
        private readonly IMongoCollection<CaptainCoasterParkSnapshot> parksCollection;
        private readonly IMongoCollection<CaptainCoasterCoasterSnapshot> coastersCollection;
        private readonly IMongoCollection<CaptainCoasterSyncSession> sessionsCollection;
        private readonly IMongoCollection<CaptainCoasterComparisonResult> comparisonCollection;
        private readonly IMongoCollection<Park> localParksCollection;
        private readonly IMongoCollection<ParkItem> localParkItemsCollection;
        private readonly IMongoCollection<AttractionManufacturer> manufacturersCollection;
        private readonly SemaphoreSlim syncSemaphore;

        public CaptainCoasterAdminService(IMongoDatabase database, IMongoDbSettings mongoDbSettings)
        {
            settingsCollection = database.GetCollection<CaptainCoasterDataSourceSettings>(mongoDbSettings.CaptainCoasterSettingsCollectionName);
            parksCollection = database.GetCollection<CaptainCoasterParkSnapshot>(mongoDbSettings.CaptainCoasterParksCollectionName);
            coastersCollection = database.GetCollection<CaptainCoasterCoasterSnapshot>(mongoDbSettings.CaptainCoasterCoastersCollectionName);
            sessionsCollection = database.GetCollection<CaptainCoasterSyncSession>(mongoDbSettings.CaptainCoasterSyncSessionsCollectionName);
            comparisonCollection = database.GetCollection<CaptainCoasterComparisonResult>(mongoDbSettings.CaptainCoasterComparisonResultsCollectionName);
            localParksCollection = database.GetCollection<Park>(mongoDbSettings.ParksCollectionName);
            localParkItemsCollection = database.GetCollection<ParkItem>(mongoDbSettings.ParkItemsCollectionName);
            manufacturersCollection = database.GetCollection<AttractionManufacturer>(mongoDbSettings.AttractionManufacturersCollectionName);
            syncSemaphore = new SemaphoreSlim(1, 1);
        }

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        public async Task<CaptainCoasterDataSourceStatusResponse> GetStatusAsync()
        {
            CaptainCoasterDataSourceSettings settings = await GetOrCreateSettingsAsync();
            long totalSessions = await sessionsCollection.CountDocumentsAsync(Builders<CaptainCoasterSyncSession>.Filter.Empty);

            return new CaptainCoasterDataSourceStatusResponse
            {
                Source = settings.Source,
                IsEnabled = settings.IsEnabled,
                LastSuccessfulImportUtc = settings.LastSuccessfulSyncUtc,
                TotalSessionsCount = (int)totalSessions
            };
        }

        public async Task<CaptainCoasterSyncSessionResponse?> GetLatestSessionAsync()
        {
            CaptainCoasterSyncSession? session = await sessionsCollection
                .Find(Builders<CaptainCoasterSyncSession>.Filter.Empty)
                .SortByDescending(item => item.StartedAtUtc)
                .FirstOrDefaultAsync();

            return session == null ? null : MapSession(session);
        }

        public async Task<CaptainCoasterComparisonPagedResponse> GetComparisonResultsAsync(
            string? sessionId,
            string? entityType,
            string? changeType,
            bool? isApplied,
            int page,
            int pageSize)
        {
            int effectivePageSize = Math.Clamp(pageSize, 10, 200);
            int effectivePage = Math.Max(0, page);

            string? effectiveSessionId = sessionId;
            if (string.IsNullOrWhiteSpace(effectiveSessionId))
            {
                CaptainCoasterSyncSession? latest = await sessionsCollection
                    .Find(Builders<CaptainCoasterSyncSession>.Filter.Empty)
                    .SortByDescending(item => item.StartedAtUtc)
                    .FirstOrDefaultAsync();
                effectiveSessionId = latest?.Id;
            }

            if (string.IsNullOrWhiteSpace(effectiveSessionId))
            {
                return new CaptainCoasterComparisonPagedResponse { Page = effectivePage, PageSize = effectivePageSize };
            }

            FilterDefinition<CaptainCoasterComparisonResult> sessionFilter =
                Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.SyncSessionId, effectiveSessionId);

            Task<long> updatedTask = comparisonCollection.CountDocumentsAsync(
                sessionFilter & Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.ChangeType, "Updated"));
            Task<long> missingTask = comparisonCollection.CountDocumentsAsync(
                sessionFilter & Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.ChangeType, "MissingLocal"));
            Task<long> duplicateTask = comparisonCollection.CountDocumentsAsync(
                sessionFilter & Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.ChangeType, "DuplicateExternal"));
            Task<long> appliedTask = comparisonCollection.CountDocumentsAsync(
                sessionFilter & Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.IsApplied, true));

            FilterDefinition<CaptainCoasterComparisonResult> pagedFilter = sessionFilter;
            if (!string.IsNullOrWhiteSpace(entityType))
            {
                pagedFilter &= Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.EntityType, entityType);
            }
            if (!string.IsNullOrWhiteSpace(changeType))
            {
                pagedFilter &= Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.ChangeType, changeType);
            }
            if (isApplied.HasValue)
            {
                pagedFilter &= Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.IsApplied, isApplied.Value);
            }

            Task<long> totalTask = comparisonCollection.CountDocumentsAsync(pagedFilter);

            await Task.WhenAll(updatedTask, missingTask, duplicateTask, appliedTask, totalTask);

            List<CaptainCoasterComparisonResult> items = await comparisonCollection
                .Find(pagedFilter)
                .SortBy(item => item.EntityType)
                .ThenBy(item => item.ChangeType)
                .ThenBy(item => item.DisplayName)
                .Skip(effectivePage * effectivePageSize)
                .Limit(effectivePageSize)
                .ToListAsync();

            return new CaptainCoasterComparisonPagedResponse
            {
                Items = items.Select(MapComparison).ToList(),
                TotalCount = (int)totalTask.Result,
                Page = effectivePage,
                PageSize = effectivePageSize,
                SessionUpdatedCount = (int)updatedTask.Result,
                SessionMissingCount = (int)missingTask.Result,
                SessionDuplicateCount = (int)duplicateTask.Result,
                SessionAppliedCount = (int)appliedTask.Result
            };
        }

        public async Task<CaptainCoasterSyncSessionResponse> StartImportFromFilesAsync(
            Stream parksStream,
            Stream coastersStream,
            CancellationToken cancellationToken)
        {
            bool lockTaken = await syncSemaphore.WaitAsync(0, cancellationToken);
            if (!lockTaken)
            {
                throw new InvalidOperationException("Un import Captain Coaster est déjà en cours.");
            }

            byte[] parksBytes = await ReadStreamToBytesAsync(parksStream, cancellationToken);
            byte[] coastersBytes = await ReadStreamToBytesAsync(coastersStream, cancellationToken);

            CaptainCoasterSyncSession session = new CaptainCoasterSyncSession
            {
                Status = "Pending",
                CurrentStep = "Queued",
                Message = "Import mis en file d'attente.",
                ProgressPercentage = 0,
                StartedAtUtc = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await sessionsCollection.InsertOneAsync(session, cancellationToken: cancellationToken);
            string sessionId = session.Id;

            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteImportAsync(sessionId, parksBytes, coastersBytes, CancellationToken.None);
                }
                finally
                {
                    syncSemaphore.Release();
                }
            }, CancellationToken.None);

            return MapSession(session);
        }

        public async Task<int> ApplyComparisonResultsAsync(ApplyCaptainCoasterComparisonRequest request, CancellationToken cancellationToken)
        {
            List<CaptainCoasterComparisonResult> results;

            if (request.ApplyAll)
            {
                string? effectiveSessionId = request.SessionId;
                if (string.IsNullOrWhiteSpace(effectiveSessionId))
                {
                    CaptainCoasterSyncSession? latest = await sessionsCollection
                        .Find(Builders<CaptainCoasterSyncSession>.Filter.Empty)
                        .SortByDescending(item => item.StartedAtUtc)
                        .FirstOrDefaultAsync(cancellationToken);
                    effectiveSessionId = latest?.Id;
                }

                if (string.IsNullOrWhiteSpace(effectiveSessionId))
                {
                    return 0;
                }

                FilterDefinition<CaptainCoasterComparisonResult> filter =
                    Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.SyncSessionId, effectiveSessionId)
                    & Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.IsApplied, false)
                    & Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.RequiresManualResolution, false);

                if (!string.IsNullOrWhiteSpace(request.EntityTypeFilter))
                {
                    filter &= Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.EntityType, request.EntityTypeFilter);
                }
                if (!string.IsNullOrWhiteSpace(request.ChangeTypeFilter))
                {
                    filter &= Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.ChangeType, request.ChangeTypeFilter);
                }

                results = await comparisonCollection.Find(filter).ToListAsync(cancellationToken);
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
                    return 0;
                }

                results = await comparisonCollection.Find(item => ids.Contains(item.Id)).ToListAsync(cancellationToken);
            }

            Dictionary<string, CaptainCoasterDuplicateResolutionRequest> resolutionsByResultId = request.DuplicateResolutions
                .Where(item => !string.IsNullOrWhiteSpace(item.ComparisonResultId))
                .GroupBy(item => item.ComparisonResultId.Trim(), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);

            List<CaptainCoasterComparisonResult> orderedResults = results
                .OrderBy(item => GetEntityApplyPriority(item.EntityType))
                .ThenBy(item => item.RequiresManualResolution ? 1 : 0)
                .ThenBy(item => item.DisplayName)
                .ToList();

            int appliedCount = 0;

            foreach (CaptainCoasterComparisonResult result in orderedResults)
            {
                resolutionsByResultId.TryGetValue(result.Id, out CaptainCoasterDuplicateResolutionRequest? resolution);
                bool applied = false;

                if (string.Equals(result.EntityType, "Park", StringComparison.OrdinalIgnoreCase))
                {
                    applied = await ApplyParkResultAsync(result, resolution, cancellationToken);
                }
                else if (string.Equals(result.EntityType, "Coaster", StringComparison.OrdinalIgnoreCase))
                {
                    applied = await ApplyCoasterResultAsync(result, resolution, cancellationToken);
                }

                if (applied)
                {
                    appliedCount++;
                }
            }

            if (orderedResults.Count > 0)
            {
                CaptainCoasterSyncSession? session = await sessionsCollection
                    .Find(item => item.Id == orderedResults[0].SyncSessionId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (session != null)
                {
                    session.Metrics.AppliedChanges += appliedCount;
                    session.UpdatedAt = DateTime.UtcNow;
                    AddLog(session, "Info", $"Application manuelle : {appliedCount} changement(s) appliqué(s).");
                    await sessionsCollection.ReplaceOneAsync(item => item.Id == session.Id, session, cancellationToken: cancellationToken);
                }
            }

            return appliedCount;
        }

        // -----------------------------------------------------------------------
        // Import pipeline
        // -----------------------------------------------------------------------

        private async Task ExecuteImportAsync(string sessionId, byte[] parksBytes, byte[] coastersBytes, CancellationToken cancellationToken)
        {
            CaptainCoasterSyncSession session = await sessionsCollection.Find(item => item.Id == sessionId).FirstAsync(cancellationToken);

            try
            {
                await UpdateSessionAsync(session, "ParsingParks", "Analyse du fichier detected-parks.json.", 10, cancellationToken);
                List<CaptainCoasterParkSnapshot> parks = ParseParksFromJson(sessionId, parksBytes);
                await parksCollection.DeleteManyAsync(item => item.SyncSessionId == sessionId, cancellationToken);
                if (parks.Count > 0) { await parksCollection.InsertManyAsync(parks, cancellationToken: cancellationToken); }
                session.Metrics.ParksFetched = parks.Count;
                int parkDuplicateGroups = CountDuplicateGroups(parks.Select(item => item.CaptainCoasterId));
                if (parkDuplicateGroups > 0)
                {
                    AddLog(session, "Warn", $"{parkDuplicateGroups} doublon(s) d'identifiant parc détecté(s) dans le staging. Une résolution humaine sera demandée.");
                }
                AddLog(session, "Info", $"{parks.Count} parc(s) parsé(s).");
                await PersistSessionAsync(session, cancellationToken);

                await UpdateSessionAsync(session, "ParsingCoasters", "Analyse du fichier coasters.json.", 40, cancellationToken);
                List<CaptainCoasterCoasterSnapshot> coasters = ParseCoastersFromJson(sessionId, coastersBytes);
                await coastersCollection.DeleteManyAsync(item => item.SyncSessionId == sessionId, cancellationToken);
                if (coasters.Count > 0) { await coastersCollection.InsertManyAsync(coasters, cancellationToken: cancellationToken); }
                session.Metrics.CoastersFetched = coasters.Count;
                int coasterDuplicateGroups = CountDuplicateGroups(coasters.Select(item => item.CaptainCoasterId));
                if (coasterDuplicateGroups > 0)
                {
                    AddLog(session, "Warn", $"{coasterDuplicateGroups} doublon(s) d'identifiant coaster détecté(s) dans le staging. Une résolution humaine sera demandée.");
                }
                AddLog(session, "Info", $"{coasters.Count} coaster(s) parsé(s).");
                await PersistSessionAsync(session, cancellationToken);

                await UpdateSessionAsync(session, "BuildingComparison", "Construction du rapport de comparaison.", 70, cancellationToken);
                List<CaptainCoasterComparisonResult> comparisonResults = await BuildComparisonResultsAsync(sessionId, parks, coasters, cancellationToken);
                await comparisonCollection.DeleteManyAsync(item => item.SyncSessionId == sessionId, cancellationToken);
                if (comparisonResults.Count > 0) { await comparisonCollection.InsertManyAsync(comparisonResults, cancellationToken: cancellationToken); }
                session.Metrics.ComparisonResults = comparisonResults.Count;
                session.Metrics.DuplicateConflicts = comparisonResults.Count(item => item.RequiresManualResolution);
                AddLog(session, "Info", $"{comparisonResults.Count} différence(s) détectée(s), dont {session.Metrics.DuplicateConflicts} conflit(s) nécessitant une résolution humaine.");
                await PersistSessionAsync(session, cancellationToken);

                CaptainCoasterDataSourceSettings settings = await GetOrCreateSettingsAsync();
                settings.LastSuccessfulSyncUtc = DateTime.UtcNow;
                settings.UpdatedAt = DateTime.UtcNow;
                await settingsCollection.ReplaceOneAsync(item => item.Id == settings.Id, settings, new ReplaceOptions { IsUpsert = true }, cancellationToken);

                session.Status = "Completed";
                session.CurrentStep = "Completed";
                session.Message = "Import Captain Coaster terminé avec succès.";
                session.ProgressPercentage = 100;
                session.CompletedAtUtc = DateTime.UtcNow;
                session.UpdatedAt = DateTime.UtcNow;
                AddLog(session, "Info", $"Terminé : {session.Metrics.ParksFetched} parcs, {session.Metrics.CoastersFetched} coasters, {session.Metrics.ComparisonResults} résultats.");
                await PersistSessionAsync(session, cancellationToken);
            }
            catch (Exception exception)
            {
                session.Status = "Failed";
                session.CurrentStep = "Failed";
                session.Message = exception.Message;
                session.CompletedAtUtc = DateTime.UtcNow;
                session.UpdatedAt = DateTime.UtcNow;
                AddLog(session, "Error", exception.ToString());
                await PersistSessionAsync(session, cancellationToken);
            }
        }

        // -----------------------------------------------------------------------
        // JSON parsing
        // -----------------------------------------------------------------------

        private static List<CaptainCoasterParkSnapshot> ParseParksFromJson(string sessionId, byte[] jsonBytes)
        {
            List<CaptainCoasterParkSnapshot> result = new List<CaptainCoasterParkSnapshot>();
            JsonDocument document = JsonDocument.Parse(jsonBytes);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Le fichier detected-parks.json doit être un tableau JSON.");
            }

            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                string externalId = ReadString(element, "externalId") ?? string.Empty;
                string name = ReadString(element, "name") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(name)) { continue; }

                string? countryRaw = ReadString(element, "country");
                result.Add(new CaptainCoasterParkSnapshot
                {
                    SyncSessionId = sessionId,
                    CaptainCoasterId = externalId.Trim(),
                    Name = name.Trim(),
                    Slug = ReadString(element, "slug"),
                    SourceUrl = ReadString(element, "sourceUrl"),
                    CountryRaw = countryRaw,
                    CountryCode = CountryNameMapper.ToCountryCode(countryRaw),
                    Latitude = 0d,
                    Longitude = 0d,
                    CoasterCount = ReadInt(element, "coasterCount") ?? 0,
                    SampleCoasterNames = ReadStringArray(element, "sampleCoasterNames"),
                    ScrapedAtUtc = ReadDateTime(element, "scrapedAtUtc"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            return result;
        }

        private static List<CaptainCoasterCoasterSnapshot> ParseCoastersFromJson(string sessionId, byte[] jsonBytes)
        {
            List<CaptainCoasterCoasterSnapshot> result = new List<CaptainCoasterCoasterSnapshot>();
            JsonDocument document = JsonDocument.Parse(jsonBytes);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Le fichier coasters.json doit être un tableau JSON.");
            }

            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                string externalId = ReadString(element, "externalId") ?? string.Empty;
                string name = ReadString(element, "name") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(name)) { continue; }

                result.Add(new CaptainCoasterCoasterSnapshot
                {
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
                    UpdatedAt = DateTime.UtcNow
                });
            }
            return result;
        }

        private static string? NormalizeManufacturer(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) { return null; }
            if (string.Equals(value.Trim(), "Inconnu", StringComparison.OrdinalIgnoreCase)) { return null; }
            if (string.Equals(value.Trim(), "Unknown", StringComparison.OrdinalIgnoreCase)) { return null; }
            return value.Trim();
        }

        // -----------------------------------------------------------------------
        // Comparison
        // -----------------------------------------------------------------------

        private async Task<List<CaptainCoasterComparisonResult>> BuildComparisonResultsAsync(
            string sessionId,
            IReadOnlyCollection<CaptainCoasterParkSnapshot> externalParks,
            IReadOnlyCollection<CaptainCoasterCoasterSnapshot> externalCoasters,
            CancellationToken cancellationToken)
        {
            List<CaptainCoasterComparisonResult> results = new List<CaptainCoasterComparisonResult>();
            List<Park> localParks = await localParksCollection.Find(Builders<Park>.Filter.Empty).ToListAsync(cancellationToken);
            List<ParkItem> localCoasters = await localParkItemsCollection.Find(item => item.Category == ParkItemCategory.Attraction).ToListAsync(cancellationToken);
            List<AttractionManufacturer> manufacturers = await manufacturersCollection.Find(Builders<AttractionManufacturer>.Filter.Empty).ToListAsync(cancellationToken);
            Dictionary<string, AttractionManufacturer> manufacturersById = manufacturers.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);

            IEnumerable<IGrouping<string, CaptainCoasterParkSnapshot>> parkGroups = externalParks
                .GroupBy(item => item.CaptainCoasterId, StringComparer.Ordinal);
            foreach (IGrouping<string, CaptainCoasterParkSnapshot> group in parkGroups)
            {
                List<CaptainCoasterParkSnapshot> variants = group.ToList();
                if (variants.Count == 1)
                {
                    CaptainCoasterParkSnapshot externalPark = variants[0];
                    Park? localPark = MatchPark(localParks, externalPark);
                    CaptainCoasterComparisonResult compResult = BuildParkComparison(sessionId, localPark, externalPark);
                    if (!string.Equals(compResult.ChangeType, "Identical", StringComparison.Ordinal))
                    {
                        results.Add(compResult);
                    }
                }
                else
                {
                    results.Add(BuildDuplicateParkComparison(sessionId, localParks, variants));
                }
            }

            IEnumerable<IGrouping<string, CaptainCoasterCoasterSnapshot>> coasterGroups = externalCoasters
                .GroupBy(item => item.CaptainCoasterId, StringComparer.Ordinal);
            foreach (IGrouping<string, CaptainCoasterCoasterSnapshot> group in coasterGroups)
            {
                List<CaptainCoasterCoasterSnapshot> variants = group.ToList();
                if (variants.Count == 1)
                {
                    CaptainCoasterCoasterSnapshot externalCoaster = variants[0];
                    ParkItem? localCoaster = MatchCoaster(localCoasters, localParks, externalCoaster);
                    CaptainCoasterComparisonResult compResult = BuildCoasterComparison(sessionId, localCoaster, externalCoaster, manufacturersById);
                    if (!string.Equals(compResult.ChangeType, "Identical", StringComparison.Ordinal))
                    {
                        results.Add(compResult);
                    }
                }
                else
                {
                    results.Add(BuildDuplicateCoasterComparison(sessionId, localCoasters, localParks, manufacturersById, variants));
                }
            }

            return results;
        }

        private static CaptainCoasterComparisonResult BuildParkComparison(string sessionId, Park? localPark, CaptainCoasterParkSnapshot externalPark)
        {
            List<CaptainCoasterFieldChange> changes = BuildParkChanges(localPark, externalPark);
            string changeType = localPark == null ? "MissingLocal" : (changes.Any(item => item.IsDifferent) ? "Updated" : "Identical");
            string matchConfidence = localPark == null ? "None" : "High";

            return new CaptainCoasterComparisonResult
            {
                SyncSessionId = sessionId,
                EntityType = "Park",
                ChangeType = changeType,
                DisplayName = externalPark.Name,
                LocalEntityId = localPark?.Id,
                ExternalEntityId = externalPark.CaptainCoasterId,
                MatchConfidence = matchConfidence,
                Changes = changes,
                HasExternalDuplicates = false,
                RequiresManualResolution = false,
                ResolutionStatus = "NotRequired",
                ExternalVariants = new List<CaptainCoasterExternalVariantOption>
                {
                    new CaptainCoasterExternalVariantOption
                    {
                        ExternalVariantId = externalPark.Id,
                        DisplayLabel = BuildParkVariantLabel(externalPark),
                        CandidateLocalEntityId = localPark?.Id,
                        SourceUrl = externalPark.SourceUrl,
                        IsSuggested = true,
                        Changes = changes
                    }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static CaptainCoasterComparisonResult BuildDuplicateParkComparison(
            string sessionId,
            IReadOnlyCollection<Park> localParks,
            IReadOnlyCollection<CaptainCoasterParkSnapshot> variants)
        {
            List<CaptainCoasterExternalVariantOption> options = variants
                .Select(variant => BuildParkVariantOption(localParks, variant))
                .ToList();
            MarkSuggestedVariant(options);

            CaptainCoasterExternalVariantOption? suggested = options.FirstOrDefault(item => item.IsSuggested) ?? options.FirstOrDefault();
            List<CaptainCoasterFieldChange> summaryChanges = new List<CaptainCoasterFieldChange>();
            AddChange(summaryChanges, "duplicateVariants", null, variants.Count.ToString(CultureInfo.InvariantCulture));

            return new CaptainCoasterComparisonResult
            {
                SyncSessionId = sessionId,
                EntityType = "Park",
                ChangeType = "DuplicateExternal",
                DisplayName = string.Join(" / ", variants.Select(item => item.Name).Distinct(StringComparer.Ordinal)),
                LocalEntityId = suggested?.CandidateLocalEntityId,
                ExternalEntityId = variants.ToList()[0].CaptainCoasterId,
                MatchConfidence = "Manual",
                Changes = summaryChanges,
                HasExternalDuplicates = true,
                RequiresManualResolution = true,
                ResolutionStatus = "Pending",
                ExternalVariants = options,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static CaptainCoasterComparisonResult BuildCoasterComparison(string sessionId, ParkItem? localCoaster, CaptainCoasterCoasterSnapshot externalCoaster, IReadOnlyDictionary<string, AttractionManufacturer> manufacturersById)
        {
            List<CaptainCoasterFieldChange> changes = BuildCoasterChanges(localCoaster, externalCoaster, manufacturersById);
            string changeType = localCoaster == null ? "MissingLocal" : (changes.Any(item => item.IsDifferent) ? "Updated" : "Identical");
            string matchConfidence = localCoaster == null ? "None" : "Medium";

            return new CaptainCoasterComparisonResult
            {
                SyncSessionId = sessionId,
                EntityType = "Coaster",
                ChangeType = changeType,
                DisplayName = externalCoaster.Name,
                LocalEntityId = localCoaster?.Id,
                ExternalEntityId = externalCoaster.CaptainCoasterId,
                MatchConfidence = matchConfidence,
                Changes = changes,
                HasExternalDuplicates = false,
                RequiresManualResolution = false,
                ResolutionStatus = "NotRequired",
                ExternalVariants = new List<CaptainCoasterExternalVariantOption>
                {
                    new CaptainCoasterExternalVariantOption
                    {
                        ExternalVariantId = externalCoaster.Id,
                        DisplayLabel = BuildCoasterVariantLabel(externalCoaster),
                        CandidateLocalEntityId = localCoaster?.Id,
                        SourceUrl = externalCoaster.SourceUrl,
                        IsSuggested = true,
                        Changes = changes
                    }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static CaptainCoasterComparisonResult BuildDuplicateCoasterComparison(
            string sessionId,
            IReadOnlyCollection<ParkItem> localCoasters,
            IReadOnlyCollection<Park> localParks,
            IReadOnlyDictionary<string, AttractionManufacturer> manufacturersById,
            IReadOnlyCollection<CaptainCoasterCoasterSnapshot> variants)
        {
            List<CaptainCoasterExternalVariantOption> options = variants
                .Select(variant => BuildCoasterVariantOption(localCoasters, localParks, manufacturersById, variant))
                .ToList();
            MarkSuggestedVariant(options);

            CaptainCoasterExternalVariantOption? suggested = options.FirstOrDefault(item => item.IsSuggested) ?? options.FirstOrDefault();
            List<CaptainCoasterFieldChange> summaryChanges = new List<CaptainCoasterFieldChange>();
            AddChange(summaryChanges, "duplicateVariants", null, variants.Count.ToString(CultureInfo.InvariantCulture));

            return new CaptainCoasterComparisonResult
            {
                SyncSessionId = sessionId,
                EntityType = "Coaster",
                ChangeType = "DuplicateExternal",
                DisplayName = string.Join(" / ", variants.Select(item => item.Name).Distinct(StringComparer.Ordinal)),
                LocalEntityId = suggested?.CandidateLocalEntityId,
                ExternalEntityId = variants.ToList()[0].CaptainCoasterId,
                MatchConfidence = "Manual",
                Changes = summaryChanges,
                HasExternalDuplicates = true,
                RequiresManualResolution = true,
                ResolutionStatus = "Pending",
                ExternalVariants = options,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static CaptainCoasterExternalVariantOption BuildParkVariantOption(
            IReadOnlyCollection<Park> localParks,
            CaptainCoasterParkSnapshot variant)
        {
            Park? localPark = MatchPark(localParks, variant);
            return new CaptainCoasterExternalVariantOption
            {
                ExternalVariantId = variant.Id,
                DisplayLabel = BuildParkVariantLabel(variant),
                CandidateLocalEntityId = localPark?.Id,
                SourceUrl = variant.SourceUrl,
                Changes = BuildParkChanges(localPark, variant)
            };
        }

        private static CaptainCoasterExternalVariantOption BuildCoasterVariantOption(
            IReadOnlyCollection<ParkItem> localCoasters,
            IReadOnlyCollection<Park> localParks,
            IReadOnlyDictionary<string, AttractionManufacturer> manufacturersById,
            CaptainCoasterCoasterSnapshot variant)
        {
            ParkItem? localCoaster = MatchCoaster(localCoasters, localParks, variant);
            return new CaptainCoasterExternalVariantOption
            {
                ExternalVariantId = variant.Id,
                DisplayLabel = BuildCoasterVariantLabel(variant),
                CandidateLocalEntityId = localCoaster?.Id,
                SourceUrl = variant.SourceUrl,
                Changes = BuildCoasterChanges(localCoaster, variant, manufacturersById)
            };
        }

        private static void MarkSuggestedVariant(List<CaptainCoasterExternalVariantOption> options)
        {
            if (options.Count == 0)
            {
                return;
            }

            List<CaptainCoasterExternalVariantOption> matchingLocal = options
                .Where(item => !string.IsNullOrWhiteSpace(item.CandidateLocalEntityId))
                .ToList();

            CaptainCoasterExternalVariantOption? suggested = null;
            if (matchingLocal.Count == 1)
            {
                suggested = matchingLocal[0];
            }
            else
            {
                suggested = options
                    .OrderBy(item => item.Changes.Count(change => change.IsDifferent))
                    .ThenBy(item => item.DisplayLabel)
                    .FirstOrDefault();
            }

            if (suggested != null)
            {
                suggested.IsSuggested = true;
            }
        }

        private static List<CaptainCoasterFieldChange> BuildParkChanges(Park? localPark, CaptainCoasterParkSnapshot externalPark)
        {
            List<CaptainCoasterFieldChange> changes = new List<CaptainCoasterFieldChange>();
            AddChange(changes, "name", localPark?.Name, externalPark.Name);
            AddChange(changes, "countryCode", localPark?.CountryCode, externalPark.CountryCode);
            return changes;
        }

        private static List<CaptainCoasterFieldChange> BuildCoasterChanges(ParkItem? localCoaster, CaptainCoasterCoasterSnapshot externalCoaster, IReadOnlyDictionary<string, AttractionManufacturer> manufacturersById)
        {
            List<CaptainCoasterFieldChange> changes = new List<CaptainCoasterFieldChange>();
            AddChange(changes, "name", localCoaster?.Name, externalCoaster.Name);
            string? localManufacturerName = ResolveManufacturerName(localCoaster?.AttractionDetails?.ManufacturerId, manufacturersById);
            AddChange(changes, "manufacturer", localManufacturerName, externalCoaster.Manufacturer);
            AddChange(changes, "model", localCoaster?.AttractionDetails?.Model, externalCoaster.Model);
            AddChange(changes, "externalSource", localCoaster?.AttractionDetails?.ExternalSource, "CaptainCoaster");
            AddChange(changes, "externalId", localCoaster?.AttractionDetails?.ExternalId, externalCoaster.CaptainCoasterId);
            AddChange(changes, "sourceUrl", localCoaster?.AttractionDetails?.SourceUrl, externalCoaster.SourceUrl);
            AddChange(changes, "status", localCoaster?.AttractionDetails?.Status, externalCoaster.Status);
            AddChange(changes, "materialType", localCoaster?.AttractionDetails?.MaterialType, externalCoaster.MaterialType);
            AddChange(changes, "seatingType", localCoaster?.AttractionDetails?.SeatingType, externalCoaster.SeatingType);
            AddChange(changes, "launchType", localCoaster?.AttractionDetails?.LaunchType, externalCoaster.LaunchType);
            AddChange(changes, "restraintType", localCoaster?.AttractionDetails?.RestraintType, externalCoaster.Restraint);
            AddChange(changes, "isLaunched", FormatBool(localCoaster?.AttractionDetails?.IsLaunched), FormatBool(externalCoaster.IsLaunched));
            AddChange(changes, "openingDate", FormatDate(localCoaster?.AttractionDetails?.OpeningDate), FormatDate(externalCoaster.OpeningDate));
            AddChange(changes, "closingDate", FormatDate(localCoaster?.AttractionDetails?.ClosingDate), FormatDate(externalCoaster.ClosingDate));
            AddChange(changes, "heightInFeet", FormatDouble(localCoaster?.AttractionDetails?.HeightInFeet), FormatDouble(ConvertMetersToFeet(externalCoaster.HeightInMeters)));
            AddChange(changes, "heightInMeters", FormatDouble(localCoaster?.AttractionDetails?.HeightInMeters), FormatDouble(externalCoaster.HeightInMeters));
            AddChange(changes, "lengthInFeet", FormatDouble(localCoaster?.AttractionDetails?.LengthInFeet), FormatDouble(ConvertMetersToFeet(externalCoaster.LengthInMeters)));
            AddChange(changes, "lengthInMeters", FormatDouble(localCoaster?.AttractionDetails?.LengthInMeters), FormatDouble(externalCoaster.LengthInMeters));
            AddChange(changes, "speedInMph", FormatDouble(localCoaster?.AttractionDetails?.SpeedInMph), FormatDouble(ConvertKmHToMph(externalCoaster.SpeedInKmH)));
            AddChange(changes, "speedInKmH", FormatDouble(localCoaster?.AttractionDetails?.SpeedInKmH), FormatDouble(externalCoaster.SpeedInKmH));
            AddChange(changes, "inversionCount", localCoaster?.AttractionDetails?.InversionCount?.ToString(CultureInfo.InvariantCulture), externalCoaster.InversionCount?.ToString(CultureInfo.InvariantCulture));
            return changes;
        }

        private static string BuildParkVariantLabel(CaptainCoasterParkSnapshot externalPark)
        {
            string country = string.IsNullOrWhiteSpace(externalPark.CountryCode) ? externalPark.CountryRaw ?? "?" : externalPark.CountryCode;
            return $"{externalPark.Name} — {country}";
        }

        private static string? ResolveManufacturerName(string? manufacturerId, IReadOnlyDictionary<string, AttractionManufacturer> manufacturersById)
        {
            if (string.IsNullOrWhiteSpace(manufacturerId))
            {
                return null;
            }

            return manufacturersById.TryGetValue(manufacturerId, out AttractionManufacturer? manufacturer)
                ? manufacturer.Name
                : manufacturerId;
        }

        private static string BuildCoasterVariantLabel(CaptainCoasterCoasterSnapshot externalCoaster)
        {
            string parkName = string.IsNullOrWhiteSpace(externalCoaster.ParkName) ? "Parc inconnu" : externalCoaster.ParkName;
            string manufacturer = string.IsNullOrWhiteSpace(externalCoaster.Manufacturer) ? "Constructeur inconnu" : externalCoaster.Manufacturer;
            return $"{externalCoaster.Name} — {parkName} — {manufacturer}";
        }

        private static Park? MatchPark(IEnumerable<Park> localParks, CaptainCoasterParkSnapshot externalPark)
        {
            string normalizedName = Normalize(externalPark.Name);
            string normalizedCountryCode = Normalize(externalPark.CountryCode);
            return localParks.FirstOrDefault(item => Normalize(item.Name) == normalizedName && Normalize(item.CountryCode) == normalizedCountryCode)
                ?? localParks.FirstOrDefault(item => Normalize(item.Name) == normalizedName);
        }

        private static ParkItem? MatchCoaster(IEnumerable<ParkItem> localCoasters, IEnumerable<Park> localParks, CaptainCoasterCoasterSnapshot externalCoaster)
        {
            string normalizedName = Normalize(externalCoaster.Name);
            string? localParkId = null;
            if (!string.IsNullOrWhiteSpace(externalCoaster.ParkName))
            {
                Park? park = localParks.FirstOrDefault(item => Normalize(item.Name) == Normalize(externalCoaster.ParkName));
                localParkId = park?.Id;
            }
            return localCoasters.FirstOrDefault(item => Normalize(item.Name) == normalizedName && (string.IsNullOrWhiteSpace(localParkId) || item.ParkId == localParkId))
                ?? localCoasters.FirstOrDefault(item => Normalize(item.Name) == normalizedName);
        }

        // -----------------------------------------------------------------------
        // Apply
        // -----------------------------------------------------------------------

        private async Task<bool> ApplyParkResultAsync(
            CaptainCoasterComparisonResult result,
            CaptainCoasterDuplicateResolutionRequest? resolution,
            CancellationToken cancellationToken)
        {
            CaptainCoasterParkSnapshot? externalPark = await ResolveParkSnapshotAsync(result, resolution, cancellationToken);
            if (externalPark == null)
            {
                return false;
            }

            List<Park> localParks = await localParksCollection.Find(Builders<Park>.Filter.Empty).ToListAsync(cancellationToken);
            Park? localPark = null;
            if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
            {
                localPark = localParks.FirstOrDefault(item => item.Id == result.LocalEntityId);
            }
            localPark ??= MatchPark(localParks, externalPark);

            if (localPark == null)
            {
                localPark = new Park
                {
                    Name = externalPark.Name,
                    CountryCode = externalPark.CountryCode,
                    Latitude = externalPark.Latitude,
                    Longitude = externalPark.Longitude,
                    IsVisible = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await localParksCollection.InsertOneAsync(localPark, cancellationToken: cancellationToken);
            }
            else
            {
                localPark.Name = externalPark.Name;
                localPark.CountryCode = externalPark.CountryCode;
                localPark.Latitude = externalPark.Latitude;
                localPark.Longitude = externalPark.Longitude;
                localPark.UpdatedAt = DateTime.UtcNow;
                await localParksCollection.ReplaceOneAsync(item => item.Id == localPark.Id, localPark, cancellationToken: cancellationToken);
            }

            result.IsApplied = true;
            result.LocalEntityId = localPark.Id;
            result.AppliedExternalVariantId = externalPark.Id;
            result.ResolutionStatus = result.RequiresManualResolution ? (resolution?.Strategy ?? "SelectVariant") : "Applied";
            result.UpdatedAt = DateTime.UtcNow;
            await comparisonCollection.ReplaceOneAsync(item => item.Id == result.Id, result, cancellationToken: cancellationToken);
            return true;
        }

        private async Task<bool> ApplyCoasterResultAsync(
            CaptainCoasterComparisonResult result,
            CaptainCoasterDuplicateResolutionRequest? resolution,
            CancellationToken cancellationToken)
        {
            CaptainCoasterCoasterSnapshot? externalCoaster = await ResolveCoasterSnapshotAsync(result, resolution, cancellationToken);
            if (externalCoaster == null)
            {
                return false;
            }

            Park? park = await ResolveOrCreateLocalParkForCoasterAsync(result.SyncSessionId, externalCoaster, cancellationToken);
            if (park == null)
            {
                return false;
            }

            AttractionManufacturer? manufacturer = await ResolveManufacturerAsync(externalCoaster.Manufacturer, cancellationToken);
            List<Park> localParks = await localParksCollection.Find(Builders<Park>.Filter.Empty).ToListAsync(cancellationToken);
            List<ParkItem> localCoasters = await localParkItemsCollection
                .Find(item => item.Category == ParkItemCategory.Attraction)
                .ToListAsync(cancellationToken);

            ParkItem? localCoaster = null;
            if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
            {
                localCoaster = localCoasters.FirstOrDefault(item => item.Id == result.LocalEntityId);
            }
            localCoaster ??= MatchCoaster(localCoasters, localParks, externalCoaster);

            AttractionDetails attractionDetails = localCoaster?.AttractionDetails ?? new AttractionDetails();
            attractionDetails.ManufacturerId = manufacturer?.Id;
            attractionDetails.Model = externalCoaster.Model;
            attractionDetails.ExternalSource = "CaptainCoaster";
            attractionDetails.ExternalId = externalCoaster.CaptainCoasterId;
            attractionDetails.SourceUrl = externalCoaster.SourceUrl;
            attractionDetails.Status = externalCoaster.Status;
            attractionDetails.MaterialType = externalCoaster.MaterialType;
            attractionDetails.SeatingType = externalCoaster.SeatingType;
            attractionDetails.LaunchType = externalCoaster.LaunchType;
            attractionDetails.RestraintType = externalCoaster.Restraint;
            attractionDetails.IsLaunched = externalCoaster.IsLaunched;
            attractionDetails.OpeningDate = externalCoaster.OpeningDate;
            attractionDetails.ClosingDate = externalCoaster.ClosingDate;
            attractionDetails.HeightInFeet = ConvertMetersToFeet(externalCoaster.HeightInMeters);
            attractionDetails.HeightInMeters = externalCoaster.HeightInMeters;
            attractionDetails.LengthInFeet = ConvertMetersToFeet(externalCoaster.LengthInMeters);
            attractionDetails.LengthInMeters = externalCoaster.LengthInMeters;
            attractionDetails.SpeedInMph = ConvertKmHToMph(externalCoaster.SpeedInKmH);
            attractionDetails.SpeedInKmH = externalCoaster.SpeedInKmH;
            attractionDetails.InversionCount = externalCoaster.InversionCount;

            if (localCoaster == null)
            {
                localCoaster = new ParkItem
                {
                    ParkId = park.Id,
                    Name = externalCoaster.Name,
                    Category = ParkItemCategory.Attraction,
                    Type = ParkItemType.RollerCoaster,
                    IsVisible = false,
                    AttractionDetails = attractionDetails,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await localParkItemsCollection.InsertOneAsync(localCoaster, cancellationToken: cancellationToken);
            }
            else
            {
                localCoaster.Name = externalCoaster.Name;
                localCoaster.ParkId = park.Id;
                localCoaster.AttractionDetails = attractionDetails;
                localCoaster.UpdatedAt = DateTime.UtcNow;
                await localParkItemsCollection.ReplaceOneAsync(item => item.Id == localCoaster.Id, localCoaster, cancellationToken: cancellationToken);
            }

            result.IsApplied = true;
            result.LocalEntityId = localCoaster.Id;
            result.AppliedExternalVariantId = externalCoaster.Id;
            result.ResolutionStatus = result.RequiresManualResolution ? (resolution?.Strategy ?? "SelectVariant") : "Applied";
            result.UpdatedAt = DateTime.UtcNow;
            await comparisonCollection.ReplaceOneAsync(item => item.Id == result.Id, result, cancellationToken: cancellationToken);
            return true;
        }

        private async Task<CaptainCoasterParkSnapshot?> ResolveParkSnapshotAsync(
            CaptainCoasterComparisonResult result,
            CaptainCoasterDuplicateResolutionRequest? resolution,
            CancellationToken cancellationToken)
        {
            if (!result.RequiresManualResolution)
            {
                string? snapshotId = result.ExternalVariants.FirstOrDefault()?.ExternalVariantId;
                if (!string.IsNullOrWhiteSpace(snapshotId))
                {
                    return await parksCollection.Find(item => item.Id == snapshotId).FirstOrDefaultAsync(cancellationToken);
                }

                return await parksCollection
                    .Find(item => item.CaptainCoasterId == result.ExternalEntityId && item.SyncSessionId == result.SyncSessionId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (resolution == null)
            {
                return null;
            }

            List<CaptainCoasterParkSnapshot> parkVariants = await parksCollection
                .Find(item => item.SyncSessionId == result.SyncSessionId && item.CaptainCoasterId == result.ExternalEntityId)
                .ToListAsync(cancellationToken);
            Dictionary<string, CaptainCoasterParkSnapshot> variantsById = parkVariants
                .ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);

            if (variantsById.Count == 0)
            {
                return null;
            }

            if (string.Equals(resolution.Strategy, "Merge", StringComparison.OrdinalIgnoreCase))
            {
                Park? localPark = null;
                if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
                {
                    localPark = await localParksCollection.Find(item => item.Id == result.LocalEntityId).FirstOrDefaultAsync(cancellationToken);
                }
                return BuildMergedParkSnapshot(result, resolution, variantsById, localPark);
            }

            if (string.IsNullOrWhiteSpace(resolution.SelectedExternalVariantId))
            {
                return null;
            }

            variantsById.TryGetValue(resolution.SelectedExternalVariantId, out CaptainCoasterParkSnapshot? selected);
            return selected;
        }

        private async Task<CaptainCoasterCoasterSnapshot?> ResolveCoasterSnapshotAsync(
            CaptainCoasterComparisonResult result,
            CaptainCoasterDuplicateResolutionRequest? resolution,
            CancellationToken cancellationToken)
        {
            if (!result.RequiresManualResolution)
            {
                string? snapshotId = result.ExternalVariants.FirstOrDefault()?.ExternalVariantId;
                if (!string.IsNullOrWhiteSpace(snapshotId))
                {
                    return await coastersCollection.Find(item => item.Id == snapshotId).FirstOrDefaultAsync(cancellationToken);
                }

                return await coastersCollection
                    .Find(item => item.CaptainCoasterId == result.ExternalEntityId && item.SyncSessionId == result.SyncSessionId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (resolution == null)
            {
                return null;
            }

            List<CaptainCoasterCoasterSnapshot> coasterVariants = await coastersCollection
                .Find(item => item.SyncSessionId == result.SyncSessionId && item.CaptainCoasterId == result.ExternalEntityId)
                .ToListAsync(cancellationToken);
            Dictionary<string, CaptainCoasterCoasterSnapshot> variantsById = coasterVariants
                .ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);

            if (variantsById.Count == 0)
            {
                return null;
            }

            if (string.Equals(resolution.Strategy, "Merge", StringComparison.OrdinalIgnoreCase))
            {
                ParkItem? localCoaster = null;
                if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
                {
                    localCoaster = await localParkItemsCollection.Find(item => item.Id == result.LocalEntityId).FirstOrDefaultAsync(cancellationToken);
                }
                return BuildMergedCoasterSnapshot(result, resolution, variantsById, localCoaster);
            }

            if (string.IsNullOrWhiteSpace(resolution.SelectedExternalVariantId))
            {
                return null;
            }

            variantsById.TryGetValue(resolution.SelectedExternalVariantId, out CaptainCoasterCoasterSnapshot? selected);
            return selected;
        }

        private static CaptainCoasterParkSnapshot? BuildMergedParkSnapshot(
            CaptainCoasterComparisonResult result,
            CaptainCoasterDuplicateResolutionRequest resolution,
            IReadOnlyDictionary<string, CaptainCoasterParkSnapshot> variantsById,
            Park? localPark)
        {
            CaptainCoasterParkSnapshot? baseVariant = GetBaseParkVariant(result, resolution, variantsById);
            if (baseVariant == null)
            {
                return null;
            }

            CaptainCoasterParkSnapshot merged = CloneParkSnapshot(baseVariant);
            foreach (CaptainCoasterFieldResolutionRequest fieldResolution in resolution.FieldResolutions)
            {
                ApplyParkFieldResolution(merged, fieldResolution, variantsById, localPark);
            }

            merged.Id = Guid.NewGuid().ToString();
            merged.CreatedAt = DateTime.UtcNow;
            merged.UpdatedAt = DateTime.UtcNow;
            return merged;
        }

        private static CaptainCoasterCoasterSnapshot? BuildMergedCoasterSnapshot(
            CaptainCoasterComparisonResult result,
            CaptainCoasterDuplicateResolutionRequest resolution,
            IReadOnlyDictionary<string, CaptainCoasterCoasterSnapshot> variantsById,
            ParkItem? localCoaster)
        {
            CaptainCoasterCoasterSnapshot? baseVariant = GetBaseCoasterVariant(result, resolution, variantsById);
            if (baseVariant == null)
            {
                return null;
            }

            CaptainCoasterCoasterSnapshot merged = CloneCoasterSnapshot(baseVariant);
            foreach (CaptainCoasterFieldResolutionRequest fieldResolution in resolution.FieldResolutions)
            {
                ApplyCoasterFieldResolution(merged, fieldResolution, variantsById, localCoaster);
            }

            merged.Id = Guid.NewGuid().ToString();
            merged.CreatedAt = DateTime.UtcNow;
            merged.UpdatedAt = DateTime.UtcNow;
            return merged;
        }

        private static CaptainCoasterParkSnapshot? GetBaseParkVariant(
            CaptainCoasterComparisonResult result,
            CaptainCoasterDuplicateResolutionRequest resolution,
            IReadOnlyDictionary<string, CaptainCoasterParkSnapshot> variantsById)
        {
            string? candidateId = resolution.SelectedExternalVariantId
                ?? result.ExternalVariants.FirstOrDefault(item => item.IsSuggested)?.ExternalVariantId
                ?? result.ExternalVariants.FirstOrDefault()?.ExternalVariantId;
            if (string.IsNullOrWhiteSpace(candidateId))
            {
                return null;
            }

            variantsById.TryGetValue(candidateId, out CaptainCoasterParkSnapshot? variant);
            return variant;
        }

        private static CaptainCoasterCoasterSnapshot? GetBaseCoasterVariant(
            CaptainCoasterComparisonResult result,
            CaptainCoasterDuplicateResolutionRequest resolution,
            IReadOnlyDictionary<string, CaptainCoasterCoasterSnapshot> variantsById)
        {
            string? candidateId = resolution.SelectedExternalVariantId
                ?? result.ExternalVariants.FirstOrDefault(item => item.IsSuggested)?.ExternalVariantId
                ?? result.ExternalVariants.FirstOrDefault()?.ExternalVariantId;
            if (string.IsNullOrWhiteSpace(candidateId))
            {
                return null;
            }

            variantsById.TryGetValue(candidateId, out CaptainCoasterCoasterSnapshot? variant);
            return variant;
        }

        private static CaptainCoasterParkSnapshot CloneParkSnapshot(CaptainCoasterParkSnapshot source)
        {
            return new CaptainCoasterParkSnapshot
            {
                SyncSessionId = source.SyncSessionId,
                CaptainCoasterId = source.CaptainCoasterId,
                Name = source.Name,
                Slug = source.Slug,
                SourceUrl = source.SourceUrl,
                CountryCode = source.CountryCode,
                CountryRaw = source.CountryRaw,
                Latitude = source.Latitude,
                Longitude = source.Longitude,
                CoasterCount = source.CoasterCount,
                SampleCoasterNames = source.SampleCoasterNames.ToList(),
                ScrapedAtUtc = source.ScrapedAtUtc
            };
        }

        private static CaptainCoasterCoasterSnapshot CloneCoasterSnapshot(CaptainCoasterCoasterSnapshot source)
        {
            return new CaptainCoasterCoasterSnapshot
            {
                SyncSessionId = source.SyncSessionId,
                CaptainCoasterId = source.CaptainCoasterId,
                Name = source.Name,
                Slug = source.Slug,
                SourceUrl = source.SourceUrl,
                ParkCaptainCoasterId = source.ParkCaptainCoasterId,
                ParkName = source.ParkName,
                Manufacturer = source.Manufacturer,
                Model = source.Model,
                MaterialType = source.MaterialType,
                SeatingType = source.SeatingType,
                LaunchType = source.LaunchType,
                Restraint = source.Restraint,
                IsLaunched = source.IsLaunched,
                SpeedInKmH = source.SpeedInKmH,
                HeightInMeters = source.HeightInMeters,
                LengthInMeters = source.LengthInMeters,
                DropInMeters = source.DropInMeters,
                InversionCount = source.InversionCount,
                Status = source.Status,
                OpeningDate = source.OpeningDate,
                ClosingDate = source.ClosingDate,
                ScrapedAtUtc = source.ScrapedAtUtc
            };
        }

        private static void ApplyParkFieldResolution(
            CaptainCoasterParkSnapshot target,
            CaptainCoasterFieldResolutionRequest fieldResolution,
            IReadOnlyDictionary<string, CaptainCoasterParkSnapshot> variantsById,
            Park? localPark)
        {
            if (string.Equals(fieldResolution.SourceType, "Local", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(fieldResolution.Field, "name", StringComparison.OrdinalIgnoreCase))
                {
                    target.Name = localPark?.Name ?? target.Name;
                }
                else if (string.Equals(fieldResolution.Field, "countryCode", StringComparison.OrdinalIgnoreCase))
                {
                    target.CountryCode = localPark?.CountryCode ?? target.CountryCode;
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(fieldResolution.ExternalVariantId))
            {
                return;
            }
            if (!variantsById.TryGetValue(fieldResolution.ExternalVariantId, out CaptainCoasterParkSnapshot? source))
            {
                return;
            }

            if (string.Equals(fieldResolution.Field, "name", StringComparison.OrdinalIgnoreCase))
            {
                target.Name = source.Name;
            }
            else if (string.Equals(fieldResolution.Field, "countryCode", StringComparison.OrdinalIgnoreCase))
            {
                target.CountryCode = source.CountryCode;
            }
        }

        private static void ApplyCoasterFieldResolution(
            CaptainCoasterCoasterSnapshot target,
            CaptainCoasterFieldResolutionRequest fieldResolution,
            IReadOnlyDictionary<string, CaptainCoasterCoasterSnapshot> variantsById,
            ParkItem? localCoaster)
        {
            if (string.Equals(fieldResolution.SourceType, "Local", StringComparison.OrdinalIgnoreCase))
            {
                ApplyLocalCoasterField(target, fieldResolution.Field, localCoaster);
                return;
            }

            if (string.IsNullOrWhiteSpace(fieldResolution.ExternalVariantId))
            {
                return;
            }
            if (!variantsById.TryGetValue(fieldResolution.ExternalVariantId, out CaptainCoasterCoasterSnapshot? source))
            {
                return;
            }

            ApplyExternalCoasterField(target, fieldResolution.Field, source);
        }

        private static void ApplyLocalCoasterField(CaptainCoasterCoasterSnapshot target, string field, ParkItem? localCoaster)
        {
            AttractionDetails? details = localCoaster?.AttractionDetails;
            if (string.Equals(field, "name", StringComparison.OrdinalIgnoreCase)) { target.Name = localCoaster?.Name ?? target.Name; }
            else if (string.Equals(field, "model", StringComparison.OrdinalIgnoreCase)) { target.Model = details?.Model; }
            else if (string.Equals(field, "sourceUrl", StringComparison.OrdinalIgnoreCase)) { target.SourceUrl = details?.SourceUrl; }
            else if (string.Equals(field, "status", StringComparison.OrdinalIgnoreCase)) { target.Status = details?.Status; }
            else if (string.Equals(field, "materialType", StringComparison.OrdinalIgnoreCase)) { target.MaterialType = details?.MaterialType; }
            else if (string.Equals(field, "seatingType", StringComparison.OrdinalIgnoreCase)) { target.SeatingType = details?.SeatingType; }
            else if (string.Equals(field, "launchType", StringComparison.OrdinalIgnoreCase)) { target.LaunchType = details?.LaunchType; }
            else if (string.Equals(field, "restraintType", StringComparison.OrdinalIgnoreCase)) { target.Restraint = details?.RestraintType; }
            else if (string.Equals(field, "isLaunched", StringComparison.OrdinalIgnoreCase)) { target.IsLaunched = details?.IsLaunched ?? target.IsLaunched; }
            else if (string.Equals(field, "openingDate", StringComparison.OrdinalIgnoreCase)) { target.OpeningDate = details?.OpeningDate; }
            else if (string.Equals(field, "closingDate", StringComparison.OrdinalIgnoreCase)) { target.ClosingDate = details?.ClosingDate; }
            else if (string.Equals(field, "heightInMeters", StringComparison.OrdinalIgnoreCase)) { target.HeightInMeters = details?.HeightInMeters; }
            else if (string.Equals(field, "lengthInMeters", StringComparison.OrdinalIgnoreCase)) { target.LengthInMeters = details?.LengthInMeters; }
            else if (string.Equals(field, "speedInKmH", StringComparison.OrdinalIgnoreCase)) { target.SpeedInKmH = details?.SpeedInKmH; }
            else if (string.Equals(field, "inversionCount", StringComparison.OrdinalIgnoreCase)) { target.InversionCount = details?.InversionCount; }
        }

        private static void ApplyExternalCoasterField(CaptainCoasterCoasterSnapshot target, string field, CaptainCoasterCoasterSnapshot source)
        {
            if (string.Equals(field, "parkName", StringComparison.OrdinalIgnoreCase)) { target.ParkName = source.ParkName; target.ParkCaptainCoasterId = source.ParkCaptainCoasterId; }
            else if (string.Equals(field, "name", StringComparison.OrdinalIgnoreCase)) { target.Name = source.Name; }
            else if (string.Equals(field, "manufacturer", StringComparison.OrdinalIgnoreCase)) { target.Manufacturer = source.Manufacturer; }
            else if (string.Equals(field, "model", StringComparison.OrdinalIgnoreCase)) { target.Model = source.Model; }
            else if (string.Equals(field, "sourceUrl", StringComparison.OrdinalIgnoreCase)) { target.SourceUrl = source.SourceUrl; }
            else if (string.Equals(field, "status", StringComparison.OrdinalIgnoreCase)) { target.Status = source.Status; }
            else if (string.Equals(field, "materialType", StringComparison.OrdinalIgnoreCase)) { target.MaterialType = source.MaterialType; }
            else if (string.Equals(field, "seatingType", StringComparison.OrdinalIgnoreCase)) { target.SeatingType = source.SeatingType; }
            else if (string.Equals(field, "launchType", StringComparison.OrdinalIgnoreCase)) { target.LaunchType = source.LaunchType; }
            else if (string.Equals(field, "restraintType", StringComparison.OrdinalIgnoreCase)) { target.Restraint = source.Restraint; }
            else if (string.Equals(field, "isLaunched", StringComparison.OrdinalIgnoreCase)) { target.IsLaunched = source.IsLaunched; }
            else if (string.Equals(field, "openingDate", StringComparison.OrdinalIgnoreCase)) { target.OpeningDate = source.OpeningDate; }
            else if (string.Equals(field, "closingDate", StringComparison.OrdinalIgnoreCase)) { target.ClosingDate = source.ClosingDate; }
            else if (string.Equals(field, "heightInMeters", StringComparison.OrdinalIgnoreCase)) { target.HeightInMeters = source.HeightInMeters; }
            else if (string.Equals(field, "lengthInMeters", StringComparison.OrdinalIgnoreCase)) { target.LengthInMeters = source.LengthInMeters; }
            else if (string.Equals(field, "speedInKmH", StringComparison.OrdinalIgnoreCase)) { target.SpeedInKmH = source.SpeedInKmH; }
            else if (string.Equals(field, "inversionCount", StringComparison.OrdinalIgnoreCase)) { target.InversionCount = source.InversionCount; }
        }

        private async Task<Park?> ResolveOrCreateLocalParkForCoasterAsync(
            string sessionId,
            CaptainCoasterCoasterSnapshot externalCoaster,
            CancellationToken cancellationToken)
        {
            List<Park> localParks = await localParksCollection.Find(Builders<Park>.Filter.Empty).ToListAsync(cancellationToken);
            Park? localPark = null;

            if (!string.IsNullOrWhiteSpace(externalCoaster.ParkCaptainCoasterId))
            {
                CaptainCoasterParkSnapshot? externalPark = await parksCollection
                    .Find(item => item.SyncSessionId == sessionId && item.CaptainCoasterId == externalCoaster.ParkCaptainCoasterId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (externalPark != null)
                {
                    localPark = MatchPark(localParks, externalPark);
                    if (localPark == null)
                    {
                        localPark = new Park
                        {
                            Name = externalPark.Name,
                            CountryCode = externalPark.CountryCode,
                            Latitude = externalPark.Latitude,
                            Longitude = externalPark.Longitude,
                            IsVisible = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await localParksCollection.InsertOneAsync(localPark, cancellationToken: cancellationToken);
                    }
                }
            }

            if (localPark == null && !string.IsNullOrWhiteSpace(externalCoaster.ParkName))
            {
                localPark = localParks.FirstOrDefault(item => Normalize(item.Name) == Normalize(externalCoaster.ParkName));
            }

            if (localPark == null && !string.IsNullOrWhiteSpace(externalCoaster.ParkName))
            {
                localPark = new Park
                {
                    Name = externalCoaster.ParkName,
                    CountryCode = null,
                    Latitude = 0d,
                    Longitude = 0d,
                    IsVisible = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await localParksCollection.InsertOneAsync(localPark, cancellationToken: cancellationToken);
            }

            return localPark;
        }

        private async Task<AttractionManufacturer?> ResolveManufacturerAsync(string? manufacturerName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(manufacturerName))
            {
                return null;
            }

            List<AttractionManufacturer> manufacturers = await manufacturersCollection.Find(Builders<AttractionManufacturer>.Filter.Empty).ToListAsync(cancellationToken);
            AttractionManufacturer? manufacturer = manufacturers.FirstOrDefault(item => Normalize(item.Name) == Normalize(manufacturerName));
            if (manufacturer != null)
            {
                return manufacturer;
            }

            manufacturer = new AttractionManufacturer
            {
                Name = manufacturerName.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await manufacturersCollection.InsertOneAsync(manufacturer, cancellationToken: cancellationToken);
            return manufacturer;
        }

        private static int GetEntityApplyPriority(string entityType)
        {
            if (string.Equals(entityType, "Park", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }
            if (string.Equals(entityType, "Coaster", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            return 99;
        }
        // -----------------------------------------------------------------------
        // Session helpers
        // -----------------------------------------------------------------------

        private async Task<CaptainCoasterDataSourceSettings> GetOrCreateSettingsAsync()
        {
            CaptainCoasterDataSourceSettings? settings = await settingsCollection.Find(item => item.Source == "CaptainCoaster").FirstOrDefaultAsync();
            if (settings != null) { return settings; }
            settings = new CaptainCoasterDataSourceSettings { Source = "CaptainCoaster", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            await settingsCollection.InsertOneAsync(settings);
            return settings;
        }

        private async Task UpdateSessionAsync(CaptainCoasterSyncSession session, string status, string message, int progressPercentage, CancellationToken cancellationToken)
        {
            session.Status = status;
            session.CurrentStep = status;
            session.Message = message;
            session.ProgressPercentage = progressPercentage;
            session.UpdatedAt = DateTime.UtcNow;
            AddLog(session, "Info", message);
            await PersistSessionAsync(session, cancellationToken);
        }

        private async Task PersistSessionAsync(CaptainCoasterSyncSession session, CancellationToken cancellationToken)
        {
            await sessionsCollection.ReplaceOneAsync(item => item.Id == session.Id, session, cancellationToken: cancellationToken);
        }

        private static void AddLog(CaptainCoasterSyncSession session, string level, string message)
        {
            session.Logs.Add(new CaptainCoasterSyncLogEntry { Level = level, Message = message, OccurredAtUtc = DateTime.UtcNow });
            if (session.Logs.Count > 200)
            {
                session.Logs = session.Logs.OrderByDescending(item => item.OccurredAtUtc).Take(200).OrderBy(item => item.OccurredAtUtc).ToList();
            }
        }

        private static void AddChange(List<CaptainCoasterFieldChange> changes, string field, string? localValue, string? externalValue)
        {
            string? normalizedLocal = string.IsNullOrWhiteSpace(localValue) ? null : localValue.Trim();
            string? normalizedExternal = string.IsNullOrWhiteSpace(externalValue) ? null : externalValue.Trim();
            changes.Add(new CaptainCoasterFieldChange
            {
                Field = field,
                LocalValue = localValue,
                ExternalValue = externalValue,
                IsDifferent = !string.Equals(normalizedLocal, normalizedExternal, StringComparison.OrdinalIgnoreCase)
            });
        }

        // -----------------------------------------------------------------------
        // JSON primitive readers
        // -----------------------------------------------------------------------

        private static string? ReadString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value)) { return null; }
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                _ => null
            };
        }

        private static double? ReadDouble(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value)) { return null; }
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double r)) { return r; }
            if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double p)) { return p; }
            return null;
        }

        private static int? ReadInt(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value)) { return null; }
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int r)) { return r; }
            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int p)) { return p; }
            return null;
        }

        private static bool? ReadBool(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value)) { return null; }
            if (value.ValueKind == JsonValueKind.True) { return true; }
            if (value.ValueKind == JsonValueKind.False) { return false; }
            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool p)) { return p; }
            return null;
        }

        private static DateTime? ReadDateTime(JsonElement element, string propertyName)
        {
            string? raw = ReadString(element, propertyName);
            if (string.IsNullOrWhiteSpace(raw)) { return null; }
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime result)) { return result; }
            return null;
        }

        private static List<string> ReadStringArray(JsonElement element, string propertyName)
        {
            List<string> result = new List<string>();
            if (!element.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind != JsonValueKind.Array) { return result; }
            foreach (JsonElement item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string? str = item.GetString();
                    if (!string.IsNullOrWhiteSpace(str)) { result.Add(str); }
                }
            }
            return result;
        }

        private static async Task<byte[]> ReadStreamToBytesAsync(Stream stream, CancellationToken cancellationToken)
        {
            using MemoryStream ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            return ms.ToArray();
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) { return string.Empty; }
            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char character in value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD))
            {
                if (char.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                }
            }
            return builder.ToString();
        }

        private static int CountDuplicateGroups(IEnumerable<string> values)
        {
            return values
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .GroupBy(item => item.Trim(), StringComparer.Ordinal)
                .Count(group => group.Count() > 1);
        }

        private static double? ConvertMetersToFeet(double? value) => value == null ? null : Math.Round(value.Value * 3.28084d, 2, MidpointRounding.AwayFromZero);
        private static double? ConvertKmHToMph(double? value) => value == null ? null : Math.Round(value.Value * 0.621371d, 2, MidpointRounding.AwayFromZero);
        private static string? FormatDouble(double? value) => value?.ToString(CultureInfo.InvariantCulture);
        private static string? FormatDate(DateTime? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string? FormatBool(bool? value) => value?.ToString();

        private static CaptainCoasterSyncSessionResponse MapSession(CaptainCoasterSyncSession session) =>
            new CaptainCoasterSyncSessionResponse
            {
                Id = session.Id,
                Status = session.Status,
                ProgressPercentage = session.ProgressPercentage,
                CurrentStep = session.CurrentStep,
                Message = session.Message,
                StartedAtUtc = session.StartedAtUtc,
                CompletedAtUtc = session.CompletedAtUtc,
                ParksFetched = session.Metrics.ParksFetched,
                CoastersFetched = session.Metrics.CoastersFetched,
                ComparisonResults = session.Metrics.ComparisonResults,
                AppliedChanges = session.Metrics.AppliedChanges,
                DuplicateConflicts = session.Metrics.DuplicateConflicts,
                Logs = session.Logs.Select(item => new CaptainCoasterSyncLogResponse
                {
                    OccurredAtUtc = item.OccurredAtUtc,
                    Level = item.Level,
                    Message = item.Message
                }).ToList()
            };

        private static CaptainCoasterComparisonResultResponse MapComparison(CaptainCoasterComparisonResult item) =>
            new CaptainCoasterComparisonResultResponse
            {
                Id = item.Id,
                EntityType = item.EntityType,
                ChangeType = item.ChangeType,
                DisplayName = item.DisplayName,
                LocalEntityId = item.LocalEntityId,
                ExternalEntityId = item.ExternalEntityId,
                MatchConfidence = item.MatchConfidence,
                IsApplied = item.IsApplied,
                HasExternalDuplicates = item.HasExternalDuplicates,
                RequiresManualResolution = item.RequiresManualResolution,
                ResolutionStatus = item.ResolutionStatus,
                AppliedExternalVariantId = item.AppliedExternalVariantId,
                Changes = item.Changes.Select(change => new CaptainCoasterFieldChangeResponse
                {
                    Field = change.Field,
                    LocalValue = change.LocalValue,
                    ExternalValue = change.ExternalValue,
                    IsDifferent = change.IsDifferent
                }).ToList(),
                ExternalVariants = item.ExternalVariants.Select(variant => new CaptainCoasterExternalVariantResponse
                {
                    ExternalVariantId = variant.ExternalVariantId,
                    DisplayLabel = variant.DisplayLabel,
                    CandidateLocalEntityId = variant.CandidateLocalEntityId,
                    SourceUrl = variant.SourceUrl,
                    IsSuggested = variant.IsSuggested,
                    Changes = variant.Changes.Select(change => new CaptainCoasterFieldChangeResponse
                    {
                        Field = change.Field,
                        LocalValue = change.LocalValue,
                        ExternalValue = change.ExternalValue,
                        IsDifferent = change.IsDifferent
                    }).ToList()
                }).ToList()
            };
    }

    // ---------------------------------------------------------------------------
    // PartialDateParser
    // ---------------------------------------------------------------------------
    internal static class PartialDateParser
    {
        private static readonly Dictionary<string, int> MonthNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["janvier"]=1,["january"]=1,["jan"]=1,
            ["février"]=2,["february"]=2,["feb"]=2,
            ["mars"]=3,["march"]=3,["mar"]=3,
            ["avril"]=4,["april"]=4,["apr"]=4,
            ["mai"]=5,["may"]=5,
            ["juin"]=6,["june"]=6,["jun"]=6,
            ["juillet"]=7,["july"]=7,["jul"]=7,
            ["août"]=8,["aout"]=8,["august"]=8,["aug"]=8,
            ["septembre"]=9,["september"]=9,["sep"]=9,
            ["octobre"]=10,["october"]=10,["oct"]=10,
            ["novembre"]=11,["november"]=11,["nov"]=11,
            ["décembre"]=12,["decembre"]=12,["december"]=12,["dec"]=12
        };

        public static DateTime? Parse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) { return null; }
            string trimmed = raw.Trim();

            if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime full))
            {
                return full;
            }
            if (Regex.IsMatch(trimmed, @"^\d{4}-\d{2}$"))
            {
                string[] parts = trimmed.Split('-');
                if (int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m) && m >= 1 && m <= 12)
                {
                    return new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
                }
            }
            if (Regex.IsMatch(trimmed, @"^\d{4}$") && int.TryParse(trimmed, out int yearOnly) && yearOnly >= 1800 && yearOnly <= 2100)
            {
                return new DateTime(yearOnly, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            Match yearMatch = Regex.Match(trimmed, @"\b(\d{4})\b");
            if (yearMatch.Success && int.TryParse(yearMatch.Value, out int yearFromText) && yearFromText >= 1800 && yearFromText <= 2100)
            {
                foreach (KeyValuePair<string, int> entry in MonthNames)
                {
                    if (trimmed.IndexOf(entry.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return new DateTime(yearFromText, entry.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                    }
                }
                return new DateTime(yearFromText, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            return null;
        }
    }

    // ---------------------------------------------------------------------------
    // CountryNameMapper
    // ---------------------------------------------------------------------------
    internal static class CountryNameMapper
    {
        private static readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["France"]="FR",["Allemagne"]="DE",["Belgique"]="BE",["Pays-Bas"]="NL",["Espagne"]="ES",
            ["Italie"]="IT",["Royaume-Uni"]="GB",["Grande-Bretagne"]="GB",["Suisse"]="CH",["Autriche"]="AT",
            ["Portugal"]="PT",["Suède"]="SE",["Danemark"]="DK",["Finlande"]="FI",["Norvège"]="NO",
            ["Pologne"]="PL",["République tchèque"]="CZ",["Tchéquie"]="CZ",["Hongrie"]="HU",["Roumanie"]="RO",
            ["Slovaquie"]="SK",["Slovénie"]="SI",["Croatie"]="HR",["Grèce"]="GR",["Turquie"]="TR",
            ["Russie"]="RU",["Ukraine"]="UA",["États-Unis"]="US",["Etats-Unis"]="US",["USA"]="US",
            ["Canada"]="CA",["Mexique"]="MX",["Brésil"]="BR",["Argentine"]="AR",["Chili"]="CL",
            ["Colombie"]="CO",["Pérou"]="PE",["Japon"]="JP",["Chine"]="CN",["Corée du Sud"]="KR",
            ["Corée"]="KR",["Australie"]="AU",["Nouvelle-Zélande"]="NZ",["Inde"]="IN",["Thaïlande"]="TH",
            ["Malaisie"]="MY",["Singapour"]="SG",["Indonésie"]="ID",["Philippines"]="PH",["Vietnam"]="VN",
            ["Bahreïn"]="BH",["Bahrain"]="BH",["Émirats arabes unis"]="AE",["Arabie saoudite"]="SA",
            ["Qatar"]="QA",["Koweït"]="KW",["Afrique du Sud"]="ZA",
            ["Germany"]="DE",["Belgium"]="BE",["Netherlands"]="NL",["Spain"]="ES",["Italy"]="IT",
            ["United Kingdom"]="GB",["Switzerland"]="CH",["Austria"]="AT",["Sweden"]="SE",["Denmark"]="DK",
            ["Finland"]="FI",["Norway"]="NO",["Poland"]="PL",["Czech Republic"]="CZ",["Hungary"]="HU",
            ["Romania"]="RO",["Slovakia"]="SK",["Slovenia"]="SI",["Croatia"]="HR",["Greece"]="GR",
            ["Turkey"]="TR",["Russia"]="RU",["United States"]="US",["Mexico"]="MX",["Brazil"]="BR",
            ["Argentina"]="AR",["Chile"]="CL",["Japan"]="JP",["China"]="CN",["South Korea"]="KR",
            ["Australia"]="AU",["New Zealand"]="NZ",["India"]="IN",["Thailand"]="TH",["Malaysia"]="MY",
            ["Indonesia"]="ID",["South Africa"]="ZA",
            ["country.belgium"]="BE",["country.france"]="FR",["country.germany"]="DE",["country.uk"]="GB",
            ["country.usa"]="US",["country.spain"]="ES",["country.italy"]="IT",["country.netherlands"]="NL",
            ["country.poland"]="PL",["country.sweden"]="SE",["country.denmark"]="DK",["country.finland"]="FI",
            ["country.switzerland"]="CH",["country.austria"]="AT",["country.portugal"]="PT",
            ["country.japan"]="JP",["country.canada"]="CA",["country.brazil"]="BR"
        };

        public static string? ToCountryCode(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) { return null; }
            string trimmed = rawValue.Trim();
            if (Values.TryGetValue(trimmed, out string? code)) { return code; }
            if (trimmed.Length == 2) { return trimmed.ToUpperInvariant(); }
            return null;
        }
    }
}
