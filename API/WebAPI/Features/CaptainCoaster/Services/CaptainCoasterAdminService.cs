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
            // Clamp page size pour éviter les abus
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

            // Filtre de session — base pour tous les compteurs
            FilterDefinition<CaptainCoasterComparisonResult> sessionFilter =
                Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.SyncSessionId, effectiveSessionId);

            // Compteurs globaux de la session (indépendants des filtres page)
            Task<long> updatedTask = comparisonCollection.CountDocumentsAsync(
                sessionFilter & Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.ChangeType, "Updated"));
            Task<long> missingTask = comparisonCollection.CountDocumentsAsync(
                sessionFilter & Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.ChangeType, "MissingLocal"));
            Task<long> appliedTask = comparisonCollection.CountDocumentsAsync(
                sessionFilter & Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.IsApplied, true));

            // Filtre paginé (avec filtres optionnels)
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

            // Lancer tous les Count en parallèle
            await Task.WhenAll(updatedTask, missingTask, appliedTask, totalTask);

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
                // Mode "tout appliquer" : on charge depuis la base avec les filtres fournis
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
                    & Builders<CaptainCoasterComparisonResult>.Filter.Eq(item => item.IsApplied, false);

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
                // Mode sélection : IDs spécifiques
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

            int appliedCount = 0;

            foreach (CaptainCoasterComparisonResult result in results)
            {
                if (string.Equals(result.EntityType, "Park", StringComparison.OrdinalIgnoreCase))
                {
                    bool applied = await ApplyParkResultAsync(result, cancellationToken);
                    if (applied) { appliedCount++; }
                }
                else if (string.Equals(result.EntityType, "Coaster", StringComparison.OrdinalIgnoreCase))
                {
                    bool applied = await ApplyCoasterResultAsync(result, cancellationToken);
                    if (applied) { appliedCount++; }
                }
            }

            if (results.Count > 0)
            {
                CaptainCoasterSyncSession? session = await sessionsCollection
                    .Find(item => item.Id == results[0].SyncSessionId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (session != null)
                {
                    session.Metrics.AppliedChanges += appliedCount;
                    session.UpdatedAt = DateTime.UtcNow;
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
                AddLog(session, "Info", $"{parks.Count} parc(s) parsé(s).");
                await PersistSessionAsync(session, cancellationToken);

                await UpdateSessionAsync(session, "ParsingCoasters", "Analyse du fichier coasters.json.", 40, cancellationToken);
                List<CaptainCoasterCoasterSnapshot> coasters = ParseCoastersFromJson(sessionId, coastersBytes);
                await coastersCollection.DeleteManyAsync(item => item.SyncSessionId == sessionId, cancellationToken);
                if (coasters.Count > 0) { await coastersCollection.InsertManyAsync(coasters, cancellationToken: cancellationToken); }
                session.Metrics.CoastersFetched = coasters.Count;
                AddLog(session, "Info", $"{coasters.Count} coaster(s) parsé(s).");
                await PersistSessionAsync(session, cancellationToken);

                await UpdateSessionAsync(session, "BuildingComparison", "Construction du rapport de comparaison.", 70, cancellationToken);
                List<CaptainCoasterComparisonResult> comparisonResults = await BuildComparisonResultsAsync(sessionId, parks, coasters, cancellationToken);
                await comparisonCollection.DeleteManyAsync(item => item.SyncSessionId == sessionId, cancellationToken);
                if (comparisonResults.Count > 0) { await comparisonCollection.InsertManyAsync(comparisonResults, cancellationToken: cancellationToken); }
                session.Metrics.ComparisonResults = comparisonResults.Count;
                AddLog(session, "Info", $"{comparisonResults.Count} différence(s) détectée(s).");
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

            foreach (CaptainCoasterParkSnapshot externalPark in externalParks)
            {
                Park? localPark = MatchPark(localParks, externalPark);
                CaptainCoasterComparisonResult compResult = BuildParkComparison(sessionId, localPark, externalPark);
                if (!string.Equals(compResult.ChangeType, "Identical", StringComparison.Ordinal))
                {
                    results.Add(compResult);
                }
            }

            foreach (CaptainCoasterCoasterSnapshot externalCoaster in externalCoasters)
            {
                ParkItem? localCoaster = MatchCoaster(localCoasters, localParks, externalCoaster);
                CaptainCoasterComparisonResult compResult = BuildCoasterComparison(sessionId, localCoaster, externalCoaster);
                if (!string.Equals(compResult.ChangeType, "Identical", StringComparison.Ordinal))
                {
                    results.Add(compResult);
                }
            }

            return results;
        }

        private static CaptainCoasterComparisonResult BuildParkComparison(string sessionId, Park? localPark, CaptainCoasterParkSnapshot externalPark)
        {
            List<CaptainCoasterFieldChange> changes = new List<CaptainCoasterFieldChange>();
            AddChange(changes, "name", localPark?.Name, externalPark.Name);
            AddChange(changes, "countryCode", localPark?.CountryCode, externalPark.CountryCode);

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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static CaptainCoasterComparisonResult BuildCoasterComparison(string sessionId, ParkItem? localCoaster, CaptainCoasterCoasterSnapshot externalCoaster)
        {
            List<CaptainCoasterFieldChange> changes = new List<CaptainCoasterFieldChange>();
            AddChange(changes, "name", localCoaster?.Name, externalCoaster.Name);
            AddChange(changes, "manufacturer", localCoaster?.AttractionDetails?.ManufacturerId, externalCoaster.Manufacturer);
            AddChange(changes, "model", localCoaster?.AttractionDetails?.Model, externalCoaster.Model);
            AddChange(changes, "openingDate", FormatDate(localCoaster?.AttractionDetails?.OpeningDate), FormatDate(externalCoaster.OpeningDate));
            AddChange(changes, "closingDate", FormatDate(localCoaster?.AttractionDetails?.ClosingDate), FormatDate(externalCoaster.ClosingDate));
            AddChange(changes, "heightInMeters", FormatDouble(localCoaster?.AttractionDetails?.HeightInMeters), FormatDouble(externalCoaster.HeightInMeters));
            AddChange(changes, "lengthInMeters", FormatDouble(localCoaster?.AttractionDetails?.LengthInMeters), FormatDouble(externalCoaster.LengthInMeters));
            AddChange(changes, "speedInKmH", FormatDouble(localCoaster?.AttractionDetails?.SpeedInKmH), FormatDouble(externalCoaster.SpeedInKmH));
            AddChange(changes, "inversionCount", localCoaster?.AttractionDetails?.InversionCount?.ToString(CultureInfo.InvariantCulture), externalCoaster.InversionCount?.ToString(CultureInfo.InvariantCulture));

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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
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

        private async Task<bool> ApplyParkResultAsync(CaptainCoasterComparisonResult result, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(result.ExternalEntityId)) { return false; }

            CaptainCoasterParkSnapshot? externalPark = await parksCollection
                .Find(item => item.CaptainCoasterId == result.ExternalEntityId && item.SyncSessionId == result.SyncSessionId)
                .FirstOrDefaultAsync(cancellationToken);
            if (externalPark == null) { return false; }

            Park? localPark = null;
            if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
            {
                localPark = await localParksCollection.Find(item => item.Id == result.LocalEntityId).FirstOrDefaultAsync(cancellationToken);
            }

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
                localPark.UpdatedAt = DateTime.UtcNow;
                await localParksCollection.ReplaceOneAsync(item => item.Id == localPark.Id, localPark, cancellationToken: cancellationToken);
            }

            result.IsApplied = true;
            result.UpdatedAt = DateTime.UtcNow;
            await comparisonCollection.ReplaceOneAsync(item => item.Id == result.Id, result, cancellationToken: cancellationToken);
            return true;
        }

        private async Task<bool> ApplyCoasterResultAsync(CaptainCoasterComparisonResult result, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(result.ExternalEntityId)) { return false; }

            CaptainCoasterCoasterSnapshot? externalCoaster = await coastersCollection
                .Find(item => item.CaptainCoasterId == result.ExternalEntityId && item.SyncSessionId == result.SyncSessionId)
                .FirstOrDefaultAsync(cancellationToken);
            if (externalCoaster == null) { return false; }

            Park? park = null;
            if (!string.IsNullOrWhiteSpace(externalCoaster.ParkName))
            {
                park = await localParksCollection.Find(item => item.Name == externalCoaster.ParkName).FirstOrDefaultAsync(cancellationToken);
            }
            if (park == null) { return false; }

            AttractionManufacturer? manufacturer = null;
            if (!string.IsNullOrWhiteSpace(externalCoaster.Manufacturer))
            {
                manufacturer = await manufacturersCollection.Find(item => item.Name == externalCoaster.Manufacturer).FirstOrDefaultAsync(cancellationToken);
                if (manufacturer == null)
                {
                    manufacturer = new AttractionManufacturer { Name = externalCoaster.Manufacturer, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    await manufacturersCollection.InsertOneAsync(manufacturer, cancellationToken: cancellationToken);
                }
            }

            ParkItem? localCoaster = null;
            if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
            {
                localCoaster = await localParkItemsCollection.Find(item => item.Id == result.LocalEntityId).FirstOrDefaultAsync(cancellationToken);
            }

            AttractionDetails attractionDetails = localCoaster?.AttractionDetails ?? new AttractionDetails();
            attractionDetails.ManufacturerId = manufacturer?.Id;
            attractionDetails.Model = externalCoaster.Model;
            attractionDetails.OpeningDate = externalCoaster.OpeningDate;
            attractionDetails.ClosingDate = externalCoaster.ClosingDate;
            attractionDetails.HeightInMeters = externalCoaster.HeightInMeters;
            attractionDetails.LengthInMeters = externalCoaster.LengthInMeters;
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
            result.UpdatedAt = DateTime.UtcNow;
            await comparisonCollection.ReplaceOneAsync(item => item.Id == result.Id, result, cancellationToken: cancellationToken);
            return true;
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

        private static string? FormatDouble(double? value) => value?.ToString(CultureInfo.InvariantCulture);
        private static string? FormatDate(DateTime? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

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
                Changes = item.Changes.Select(change => new CaptainCoasterFieldChangeResponse
                {
                    Field = change.Field,
                    LocalValue = change.LocalValue,
                    ExternalValue = change.ExternalValue,
                    IsDifferent = change.IsDifferent
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
