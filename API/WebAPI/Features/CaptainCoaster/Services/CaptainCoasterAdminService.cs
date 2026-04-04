using System.Globalization;
using System.Text;
using System.Text.Json;
using Common.General.Localization;
using Entities.Model.Countries;
using Entities.Model.Parks;
using MongoDB.Driver;
using Repositories.Interfaces;
using WebAPI.Features.CaptainCoaster.Contracts;
using WebAPI.Features.CaptainCoaster.Models;
using WebAPI.Settings.MongoDB;

namespace WebAPI.Features.CaptainCoaster.Services
{
    public sealed class CaptainCoasterImportedFile
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public interface ICaptainCoasterAdminService
    {
        Task<IReadOnlyCollection<AdminDataSourceSummaryResponse>> GetSourcesAsync();
        Task<CaptainCoasterSettingsResponse> GetSettingsAsync();
        Task<CaptainCoasterSettingsResponse> UpdateSettingsAsync(UpdateCaptainCoasterSettingsRequest request);
        Task<CaptainCoasterSyncSessionResponse?> GetLatestSessionAsync();
        Task<IReadOnlyCollection<CaptainCoasterComparisonResultResponse>> GetComparisonResultsAsync(string? sessionId);
        Task<CaptainCoasterSyncSessionResponse> ImportJsonAsync(IReadOnlyCollection<CaptainCoasterImportedFile> files, CancellationToken cancellationToken);
        Task<int> ApplyComparisonResultsAsync(ApplyCaptainCoasterComparisonRequest request, CancellationToken cancellationToken);
    }

    public sealed class CaptainCoasterAdminService : ICaptainCoasterAdminService
    {
        private const string SourceKey = "captain-coaster";
        private const string SourceName = "CaptainCoaster";

        private readonly IMongoCollection<CaptainCoasterDataSourceSettings> settingsCollection;
        private readonly IMongoCollection<CaptainCoasterParkSnapshot> parksCollection;
        private readonly IMongoCollection<CaptainCoasterCoasterSnapshot> coastersCollection;
        private readonly IMongoCollection<CaptainCoasterSyncSession> sessionsCollection;
        private readonly IMongoCollection<CaptainCoasterComparisonResult> comparisonCollection;
        private readonly IMongoCollection<Park> localParksCollection;
        private readonly IMongoCollection<ParkItem> localParkItemsCollection;
        private readonly IMongoCollection<AttractionManufacturer> manufacturersCollection;
        private readonly IMongoCollection<Country> countriesCollection;
        private readonly SemaphoreSlim importSemaphore;

        public CaptainCoasterAdminService(
            IMongoDatabase database,
            IMongoDbSettings mongoDbSettings)
        {
            settingsCollection = database.GetCollection<CaptainCoasterDataSourceSettings>(mongoDbSettings.CaptainCoasterSettingsCollectionName);
            parksCollection = database.GetCollection<CaptainCoasterParkSnapshot>(mongoDbSettings.CaptainCoasterParksCollectionName);
            coastersCollection = database.GetCollection<CaptainCoasterCoasterSnapshot>(mongoDbSettings.CaptainCoasterCoastersCollectionName);
            sessionsCollection = database.GetCollection<CaptainCoasterSyncSession>(mongoDbSettings.CaptainCoasterSyncSessionsCollectionName);
            comparisonCollection = database.GetCollection<CaptainCoasterComparisonResult>(mongoDbSettings.CaptainCoasterComparisonResultsCollectionName);
            localParksCollection = database.GetCollection<Park>(mongoDbSettings.ParksCollectionName);
            localParkItemsCollection = database.GetCollection<ParkItem>(mongoDbSettings.ParkItemsCollectionName);
            manufacturersCollection = database.GetCollection<AttractionManufacturer>(mongoDbSettings.AttractionManufacturersCollectionName);
            countriesCollection = database.GetCollection<Country>(mongoDbSettings.CountriesCollectionName);
            importSemaphore = new SemaphoreSlim(1, 1);
        }

        public async Task<IReadOnlyCollection<AdminDataSourceSummaryResponse>> GetSourcesAsync()
        {
            CaptainCoasterDataSourceSettings settings = await GetOrCreateSettingsAsync();
            CaptainCoasterSyncSession? latestSession = await sessionsCollection.Find(item => item.SourceKey == SourceKey)
                .SortByDescending(item => item.StartedAtUtc)
                .FirstOrDefaultAsync();

            AdminDataSourceSummaryResponse summary = new()
            {
                SourceKey = SourceKey,
                DisplayName = settings.DisplayName,
                Description = settings.Description,
                InputMode = settings.InputMode,
                IsEnabled = settings.IsEnabled,
                LastSuccessfulImportUtc = settings.LastSuccessfulImportUtc,
                LastImportedParkCount = latestSession?.Metrics.ParksFetched ?? 0,
                LastImportedCoasterCount = latestSession?.Metrics.CoastersFetched ?? 0,
                LastComparisonResultCount = latestSession?.Metrics.ComparisonResults ?? 0
            };

            return new[] { summary };
        }

        public async Task<CaptainCoasterSettingsResponse> GetSettingsAsync()
        {
            CaptainCoasterDataSourceSettings settings = await GetOrCreateSettingsAsync();
            return MapSettings(settings);
        }

        public async Task<CaptainCoasterSettingsResponse> UpdateSettingsAsync(UpdateCaptainCoasterSettingsRequest request)
        {
            CaptainCoasterDataSourceSettings settings = await GetOrCreateSettingsAsync();
            settings.IsEnabled = request.IsEnabled;
            settings.UpdatedAt = DateTime.UtcNow;

            await settingsCollection.ReplaceOneAsync(item => item.Id == settings.Id, settings, new ReplaceOptions { IsUpsert = true });
            return MapSettings(settings);
        }

        public async Task<CaptainCoasterSyncSessionResponse?> GetLatestSessionAsync()
        {
            CaptainCoasterSyncSession? session = await sessionsCollection.Find(item => item.SourceKey == SourceKey)
                .SortByDescending(item => item.StartedAtUtc)
                .FirstOrDefaultAsync();

            return session == null ? null : MapSession(session);
        }

        public async Task<IReadOnlyCollection<CaptainCoasterComparisonResultResponse>> GetComparisonResultsAsync(string? sessionId)
        {
            string? effectiveSessionId = sessionId;
            if (string.IsNullOrWhiteSpace(effectiveSessionId))
            {
                CaptainCoasterSyncSession? latestSession = await sessionsCollection.Find(item => item.SourceKey == SourceKey)
                    .SortByDescending(item => item.StartedAtUtc)
                    .FirstOrDefaultAsync();
                effectiveSessionId = latestSession?.Id;
            }

            if (string.IsNullOrWhiteSpace(effectiveSessionId))
            {
                return Array.Empty<CaptainCoasterComparisonResultResponse>();
            }

            List<CaptainCoasterComparisonResult> items = await comparisonCollection.Find(item => item.SyncSessionId == effectiveSessionId)
                .SortBy(item => item.EntityType)
                .ThenBy(item => item.ChangeType)
                .ThenBy(item => item.DisplayName)
                .ToListAsync();

            return items.Select(MapComparison).ToList();
        }

        public async Task<CaptainCoasterSyncSessionResponse> ImportJsonAsync(IReadOnlyCollection<CaptainCoasterImportedFile> files, CancellationToken cancellationToken)
        {
            CaptainCoasterSyncSession? currentSession = null;
            bool lockTaken = await importSemaphore.WaitAsync(0, cancellationToken);
            if (!lockTaken)
            {
                throw new InvalidOperationException("Un import Captain Coaster est déjà en cours.");
            }

            try
            {
                CaptainCoasterDataSourceSettings settings = await GetOrCreateSettingsAsync();
                if (!settings.IsEnabled)
                {
                    throw new InvalidOperationException("La source Captain Coaster est désactivée.");
                }

                Dictionary<string, CaptainCoasterImportedFile> normalizedFiles = files
                    .Where(item => item != null && !string.IsNullOrWhiteSpace(item.FileName))
                    .GroupBy(item => NormalizeFileName(item.FileName))
                    .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

                if (!normalizedFiles.ContainsKey("coasters.json") || !normalizedFiles.ContainsKey("detected-parks.json"))
                {
                    throw new InvalidOperationException("Les fichiers coasters.json et detected-parks.json sont obligatoires.");
                }

                CaptainCoasterSyncSession session = new()
                {
                    SourceKey = SourceKey,
                    Status = "Pending",
                    CurrentStep = "Pending",
                    Message = "Préparation de l'import JSON.",
                    StartedAtUtc = DateTime.UtcNow,
                    SourceFileCount = normalizedFiles.Count,
                    SourceFileNames = normalizedFiles.Values.Select(item => item.FileName).OrderBy(item => item).ToList(),
                    ProgressPercentage = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                AddLog(session, "Info", "Import JSON Captain Coaster initialisé.");
                session.ManifestSummary = BuildManifestSummary(normalizedFiles);
                await sessionsCollection.InsertOneAsync(session, cancellationToken: cancellationToken);
                currentSession = session;

                await UpdateSessionAsync(session, "Parsing", "Lecture des fichiers JSON fournis.", 15, cancellationToken);

                Dictionary<string, string> countryCodeMap = await BuildCountryCodeMapAsync(cancellationToken);
                List<CaptainCoasterParkSnapshot> externalParks = ParseParks(normalizedFiles["detected-parks.json"].Content, session.Id, countryCodeMap);
                List<CaptainCoasterCoasterSnapshot> externalCoasters = ParseCoasters(normalizedFiles["coasters.json"].Content, session.Id);
                session.Metrics.ParksFetched = externalParks.Count;
                session.Metrics.CoastersFetched = externalCoasters.Count;

                await UpdateSessionAsync(session, "Staging", "Alimentation des collections de staging.", 40, cancellationToken);
                if (externalParks.Count > 0)
                {
                    await parksCollection.InsertManyAsync(externalParks, cancellationToken: cancellationToken);
                }

                if (externalCoasters.Count > 0)
                {
                    await coastersCollection.InsertManyAsync(externalCoasters, cancellationToken: cancellationToken);
                }

                await UpdateSessionAsync(session, "Comparing", "Analyse des rapprochements et des différences.", 70, cancellationToken);
                List<CaptainCoasterComparisonResult> comparisonResults = await BuildComparisonResultsAsync(session.Id, externalParks, externalCoasters, cancellationToken);
                session.Metrics.ComparisonResults = comparisonResults.Count;
                if (comparisonResults.Count > 0)
                {
                    await comparisonCollection.InsertManyAsync(comparisonResults, cancellationToken: cancellationToken);
                }

                settings.LastSuccessfulImportUtc = DateTime.UtcNow;
                settings.UpdatedAt = DateTime.UtcNow;
                await settingsCollection.ReplaceOneAsync(item => item.Id == settings.Id, settings, new ReplaceOptions { IsUpsert = true }, cancellationToken);

                session.Status = "Completed";
                session.CurrentStep = "Completed";
                session.Message = "Import JSON Captain Coaster terminé.";
                session.ProgressPercentage = 100;
                session.CompletedAtUtc = DateTime.UtcNow;
                session.UpdatedAt = DateTime.UtcNow;
                AddLog(session, "Info", $"Import terminé : {externalParks.Count} parc(s), {externalCoasters.Count} coaster(s), {comparisonResults.Count} différence(s)." );
                await PersistSessionAsync(session, cancellationToken);

                return MapSession(session);
            }
            catch (Exception exception)
            {
                if (currentSession != null && currentSession.CompletedAtUtc == null)
                {
                    currentSession.Status = "Failed";
                    currentSession.CurrentStep = "Failed";
                    currentSession.Message = exception.Message;
                    currentSession.ProgressPercentage = currentSession.ProgressPercentage <= 0 ? 1 : currentSession.ProgressPercentage;
                    currentSession.CompletedAtUtc = DateTime.UtcNow;
                    currentSession.UpdatedAt = DateTime.UtcNow;
                    AddLog(currentSession, "Error", exception.Message);
                    await PersistSessionAsync(currentSession, cancellationToken);
                }

                throw;
            }
            finally
            {
                importSemaphore.Release();
            }
        }

        public async Task<int> ApplyComparisonResultsAsync(ApplyCaptainCoasterComparisonRequest request, CancellationToken cancellationToken)
        {
            if (request.ComparisonResultIds == null || request.ComparisonResultIds.Count == 0)
            {
                return 0;
            }

            List<CaptainCoasterComparisonResult> results = await comparisonCollection.Find(item => request.ComparisonResultIds.Contains(item.Id)).ToListAsync(cancellationToken);
            if (results.Count == 0)
            {
                return 0;
            }

            int appliedCount = 0;
            foreach (CaptainCoasterComparisonResult result in results)
            {
                if (result.IsApplied)
                {
                    continue;
                }

                bool applied = false;
                if (string.Equals(result.EntityType, "Park", StringComparison.Ordinal))
                {
                    applied = await ApplyParkComparisonAsync(result, cancellationToken);
                }
                else if (string.Equals(result.EntityType, "Coaster", StringComparison.Ordinal))
                {
                    applied = await ApplyCoasterComparisonAsync(result, cancellationToken);
                }

                if (applied)
                {
                    appliedCount++;
                }
            }

            HashSet<string> impactedSessionIds = results.Select(item => item.SyncSessionId).ToHashSet(StringComparer.Ordinal);
            foreach (string sessionId in impactedSessionIds)
            {
                CaptainCoasterSyncSession? session = await sessionsCollection.Find(item => item.Id == sessionId).FirstOrDefaultAsync(cancellationToken);
                if (session != null)
                {
                    long appliedCountForSession = await comparisonCollection.CountDocumentsAsync(
                        item => item.SyncSessionId == sessionId && item.IsApplied,
                        cancellationToken: cancellationToken);

                    session.Metrics.AppliedChanges = (int)appliedCountForSession;
                    session.UpdatedAt = DateTime.UtcNow;
                    await PersistSessionAsync(session, cancellationToken);
                }
            }

            return appliedCount;
        }

        private async Task<List<CaptainCoasterComparisonResult>> BuildComparisonResultsAsync(
            string sessionId,
            IReadOnlyCollection<CaptainCoasterParkSnapshot> externalParks,
            IReadOnlyCollection<CaptainCoasterCoasterSnapshot> externalCoasters,
            CancellationToken cancellationToken)
        {
            List<CaptainCoasterComparisonResult> results = new();
            List<Park> localParks = await localParksCollection.Find(Builders<Park>.Filter.Empty).ToListAsync(cancellationToken);
            List<ParkItem> localCoasters = await localParkItemsCollection.Find(item => item.Category == ParkItemCategory.Attraction).ToListAsync(cancellationToken);
            List<AttractionManufacturer> manufacturers = await manufacturersCollection.Find(Builders<AttractionManufacturer>.Filter.Empty).ToListAsync(cancellationToken);
            Dictionary<string, AttractionManufacturer> manufacturersById = manufacturers
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);

            foreach (CaptainCoasterParkSnapshot externalPark in externalParks)
            {
                Park? localPark = MatchPark(localParks, externalPark);
                CaptainCoasterComparisonResult result = BuildParkComparison(sessionId, localPark, externalPark);
                if (!string.Equals(result.ChangeType, "Identical", StringComparison.Ordinal))
                {
                    results.Add(result);
                }
            }

            foreach (CaptainCoasterCoasterSnapshot externalCoaster in externalCoasters)
            {
                Park? matchedPark = MatchPark(localParks, externalCoaster.ParkName, externalCoaster.Country);
                ParkItem? localCoaster = MatchCoaster(localCoasters, manufacturersById, matchedPark, externalCoaster);
                CaptainCoasterComparisonResult result = BuildCoasterComparison(sessionId, localCoaster, externalCoaster, matchedPark, manufacturersById);
                if (!string.Equals(result.ChangeType, "Identical", StringComparison.Ordinal))
                {
                    results.Add(result);
                }
            }

            return results;
        }

        private CaptainCoasterComparisonResult BuildParkComparison(string sessionId, Park? localPark, CaptainCoasterParkSnapshot externalPark)
        {
            List<CaptainCoasterFieldChange> changes = new();
            AddChange(changes, "name", localPark?.Name, externalPark.Name);
            AddChange(changes, "countryCode", localPark?.CountryCode, externalPark.CountryCode);

            bool hasDifferences = changes.Any(item => item.IsDifferent);
            return new CaptainCoasterComparisonResult
            {
                SyncSessionId = sessionId,
                EntityType = "Park",
                ChangeType = localPark == null ? "MissingLocal" : hasDifferences ? "Updated" : "Identical",
                DisplayName = externalPark.Name,
                LocalEntityId = localPark?.Id,
                ExternalEntityId = externalPark.CaptainCoasterId,
                MatchConfidence = localPark == null ? "None" : "High",
                Changes = changes,
                IsApplied = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private CaptainCoasterComparisonResult BuildCoasterComparison(
            string sessionId,
            ParkItem? localCoaster,
            CaptainCoasterCoasterSnapshot externalCoaster,
            Park? matchedPark,
            IReadOnlyDictionary<string, AttractionManufacturer> manufacturersById)
        {
            AttractionDetails? localDetails = localCoaster?.AttractionDetails;
            string? localManufacturerName = ResolveManufacturerName(localDetails?.ManufacturerId, manufacturersById);
            string? localParkName = matchedPark?.Name;

            List<CaptainCoasterFieldChange> changes = new();
            AddChange(changes, "park", localParkName, externalCoaster.ParkName);
            AddChange(changes, "manufacturer", localManufacturerName, externalCoaster.Manufacturer);
            AddChange(changes, "model", localDetails?.Model, externalCoaster.Model);
            AddChange(changes, "externalId", localDetails?.ExternalId, externalCoaster.CaptainCoasterId);
            AddChange(changes, "sourceUrl", localDetails?.SourceUrl, externalCoaster.SourceUrl);
            AddChange(changes, "status", localDetails?.Status, externalCoaster.Status);
            AddChange(changes, "materialType", localDetails?.MaterialType, externalCoaster.MaterialType);
            AddChange(changes, "seatingType", localDetails?.SeatingType, externalCoaster.SeatingType);
            AddChange(changes, "launchType", localDetails?.LaunchType, externalCoaster.LaunchType);
            AddChange(changes, "restraintType", localDetails?.RestraintType, externalCoaster.RestraintType);
            AddChange(changes, "isLaunched", FormatBool(localDetails?.IsLaunched), FormatBool(externalCoaster.IsLaunched));
            AddChange(changes, "openingDate", FormatDate(localDetails?.OpeningDate), FormatDate(externalCoaster.OpeningDate));
            AddChange(changes, "closingDate", FormatDate(localDetails?.ClosingDate), FormatDate(externalCoaster.ClosingDate));
            AddChange(changes, "heightInFeet", FormatDouble(localDetails?.HeightInFeet), FormatDouble(externalCoaster.HeightInFeet));
            AddChange(changes, "heightInMeters", FormatDouble(localDetails?.HeightInMeters), FormatDouble(externalCoaster.HeightInMeters));
            AddChange(changes, "lengthInFeet", FormatDouble(localDetails?.LengthInFeet), FormatDouble(externalCoaster.LengthInFeet));
            AddChange(changes, "lengthInMeters", FormatDouble(localDetails?.LengthInMeters), FormatDouble(externalCoaster.LengthInMeters));
            AddChange(changes, "speedInMph", FormatDouble(localDetails?.SpeedInMph), FormatDouble(externalCoaster.SpeedInMph));
            AddChange(changes, "speedInKmH", FormatDouble(localDetails?.SpeedInKmH), FormatDouble(externalCoaster.SpeedInKmH));
            AddChange(changes, "dropInMeters", FormatDouble(localDetails?.DropInMeters), FormatDouble(externalCoaster.DropInMeters));
            AddChange(changes, "inversionCount", FormatInt(localDetails?.InversionCount), FormatInt(externalCoaster.InversionCount));

            bool hasDifferences = changes.Any(item => item.IsDifferent);
            return new CaptainCoasterComparisonResult
            {
                SyncSessionId = sessionId,
                EntityType = "Coaster",
                ChangeType = localCoaster == null ? "MissingLocal" : hasDifferences ? "Updated" : "Identical",
                DisplayName = externalCoaster.Name,
                LocalEntityId = localCoaster?.Id,
                ExternalEntityId = externalCoaster.CaptainCoasterId,
                MatchConfidence = GetCoasterMatchConfidence(localCoaster, externalCoaster),
                Changes = changes,
                IsApplied = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private async Task<bool> ApplyParkComparisonAsync(CaptainCoasterComparisonResult result, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(result.ExternalEntityId))
            {
                return false;
            }

            CaptainCoasterParkSnapshot? externalPark = await parksCollection.Find(item => item.CaptainCoasterId == result.ExternalEntityId && item.SyncSessionId == result.SyncSessionId)
                .FirstOrDefaultAsync(cancellationToken);
            if (externalPark == null)
            {
                return false;
            }

            Park? localPark = null;
            if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
            {
                localPark = await localParksCollection.Find(item => item.Id == result.LocalEntityId).FirstOrDefaultAsync(cancellationToken);
            }

            localPark ??= await localParksCollection.Find(item => item.Name == externalPark.Name && item.CountryCode == externalPark.CountryCode)
                .FirstOrDefaultAsync(cancellationToken);

            if (localPark == null)
            {
                localPark = new Park
                {
                    Name = externalPark.Name,
                    CountryCode = externalPark.CountryCode,
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
            result.LocalEntityId = localPark.Id;
            result.UpdatedAt = DateTime.UtcNow;
            await comparisonCollection.ReplaceOneAsync(item => item.Id == result.Id, result, cancellationToken: cancellationToken);
            return true;
        }

        private async Task<bool> ApplyCoasterComparisonAsync(CaptainCoasterComparisonResult result, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(result.ExternalEntityId))
            {
                return false;
            }

            CaptainCoasterCoasterSnapshot? externalCoaster = await coastersCollection.Find(item => item.CaptainCoasterId == result.ExternalEntityId && item.SyncSessionId == result.SyncSessionId)
                .FirstOrDefaultAsync(cancellationToken);
            if (externalCoaster == null)
            {
                return false;
            }

            Park? park = await ResolveOrCreateParkForCoasterAsync(externalCoaster, result.SyncSessionId, cancellationToken);
            if (park == null)
            {
                return false;
            }

            AttractionManufacturer? manufacturer = await ResolveOrCreateManufacturerAsync(externalCoaster.Manufacturer, cancellationToken);

            ParkItem? localCoaster = null;
            if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
            {
                localCoaster = await localParkItemsCollection.Find(item => item.Id == result.LocalEntityId).FirstOrDefaultAsync(cancellationToken);
            }

            AttractionDetails attractionDetails = localCoaster?.AttractionDetails ?? new AttractionDetails();
            attractionDetails.ManufacturerId = manufacturer?.Id;
            attractionDetails.Model = externalCoaster.Model;
            attractionDetails.ExternalSource = externalCoaster.ExternalSource;
            attractionDetails.ExternalId = externalCoaster.CaptainCoasterId;
            attractionDetails.SourceUrl = externalCoaster.SourceUrl;
            attractionDetails.Status = externalCoaster.Status;
            attractionDetails.MaterialType = externalCoaster.MaterialType;
            attractionDetails.SeatingType = externalCoaster.SeatingType;
            attractionDetails.LaunchType = externalCoaster.LaunchType;
            attractionDetails.RestraintType = externalCoaster.RestraintType;
            attractionDetails.IsLaunched = externalCoaster.IsLaunched;
            attractionDetails.OpeningDate = externalCoaster.OpeningDate;
            attractionDetails.ClosingDate = externalCoaster.ClosingDate;
            attractionDetails.HeightInFeet = externalCoaster.HeightInFeet;
            attractionDetails.HeightInMeters = externalCoaster.HeightInMeters;
            attractionDetails.LengthInFeet = externalCoaster.LengthInFeet;
            attractionDetails.LengthInMeters = externalCoaster.LengthInMeters;
            attractionDetails.SpeedInMph = externalCoaster.SpeedInMph;
            attractionDetails.SpeedInKmH = externalCoaster.SpeedInKmH;
            attractionDetails.DropInMeters = externalCoaster.DropInMeters;
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
                localCoaster.Category = ParkItemCategory.Attraction;
                localCoaster.Type = ParkItemType.RollerCoaster;
                localCoaster.AttractionDetails = attractionDetails;
                localCoaster.UpdatedAt = DateTime.UtcNow;
                await localParkItemsCollection.ReplaceOneAsync(item => item.Id == localCoaster.Id, localCoaster, cancellationToken: cancellationToken);
            }

            result.IsApplied = true;
            result.LocalEntityId = localCoaster.Id;
            result.UpdatedAt = DateTime.UtcNow;
            await comparisonCollection.ReplaceOneAsync(item => item.Id == result.Id, result, cancellationToken: cancellationToken);
            return true;
        }

        private async Task<Park?> ResolveOrCreateParkForCoasterAsync(CaptainCoasterCoasterSnapshot externalCoaster, string syncSessionId, CancellationToken cancellationToken)
        {
            Park? park = await localParksCollection.Find(item => item.Name == externalCoaster.ParkName).FirstOrDefaultAsync(cancellationToken);
            if (park != null)
            {
                return park;
            }

            if (string.IsNullOrWhiteSpace(externalCoaster.ParkCaptainCoasterId) && string.IsNullOrWhiteSpace(externalCoaster.ParkName))
            {
                return null;
            }

            CaptainCoasterParkSnapshot? externalPark = null;
            if (!string.IsNullOrWhiteSpace(externalCoaster.ParkCaptainCoasterId))
            {
                externalPark = await parksCollection.Find(item => item.CaptainCoasterId == externalCoaster.ParkCaptainCoasterId && item.SyncSessionId == syncSessionId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            externalPark ??= await parksCollection.Find(item => item.Name == externalCoaster.ParkName && item.SyncSessionId == syncSessionId)
                .FirstOrDefaultAsync(cancellationToken);

            if (externalPark == null)
            {
                return null;
            }

            Park? localPark = await localParksCollection.Find(item => item.Name == externalPark.Name && item.CountryCode == externalPark.CountryCode)
                .FirstOrDefaultAsync(cancellationToken);
            if (localPark != null)
            {
                return localPark;
            }

            localPark = new Park
            {
                Name = externalPark.Name,
                CountryCode = externalPark.CountryCode,
                IsVisible = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await localParksCollection.InsertOneAsync(localPark, cancellationToken: cancellationToken);
            return localPark;
        }

        private async Task<AttractionManufacturer?> ResolveOrCreateManufacturerAsync(string? manufacturerName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(manufacturerName) || string.Equals(manufacturerName.Trim(), "Inconnu", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            AttractionManufacturer? manufacturer = await manufacturersCollection.Find(item => item.Name == manufacturerName).FirstOrDefaultAsync(cancellationToken);
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

        private async Task<Dictionary<string, string>> BuildCountryCodeMapAsync(CancellationToken cancellationToken)
        {
            List<Country> countries = await countriesCollection.Find(Builders<Country>.Filter.Empty).ToListAsync(cancellationToken);
            Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
            foreach (Country country in countries)
            {
                if (!string.IsNullOrWhiteSpace(country.IsoCode))
                {
                    map[country.IsoCode.Trim()] = country.IsoCode.Trim().ToUpperInvariant();
                }

                foreach (LocalizedItem<string> name in country.Names ?? new List<LocalizedItem<string>>())
                {
                    if (!string.IsNullOrWhiteSpace(name.Value))
                    {
                        map[name.Value.Trim()] = country.IsoCode.Trim().ToUpperInvariant();
                    }
                }
            }

            foreach (KeyValuePair<string, string> item in CountryCodeMapper.Values)
            {
                map[item.Key] = item.Value;
            }

            return map;
        }

        private static List<CaptainCoasterParkSnapshot> ParseParks(string content, string sessionId, IReadOnlyDictionary<string, string> countryCodeMap)
        {
            using JsonDocument document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Le fichier detected-parks.json doit contenir un tableau JSON.");
            }

            List<CaptainCoasterParkSnapshot> items = new();
            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                string? countryRaw = ReadString(element, "country");
                CaptainCoasterParkSnapshot item = new()
                {
                    SyncSessionId = sessionId,
                    ExternalSource = ReadString(element, "externalSource") ?? SourceName,
                    CaptainCoasterId = ReadString(element, "externalId") ?? string.Empty,
                    Name = ReadString(element, "name") ?? string.Empty,
                    Slug = ReadString(element, "slug"),
                    SourceUrl = ReadString(element, "sourceUrl"),
                    CountryRaw = countryRaw,
                    CountryCode = ResolveCountryCode(countryRaw, countryCodeMap),
                    CoasterCount = ReadInt(element, "coasterCount"),
                    SampleCoasterNames = ReadStringArray(element, "sampleCoasterNames"),
                    ScrapedAtUtc = ReadDateTime(element, "scrapedAtUtc"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (string.IsNullOrWhiteSpace(item.CaptainCoasterId) || string.IsNullOrWhiteSpace(item.Name))
                {
                    continue;
                }

                items.Add(item);
            }

            return items;
        }

        private static List<CaptainCoasterCoasterSnapshot> ParseCoasters(string content, string sessionId)
        {
            using JsonDocument document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Le fichier coasters.json doit contenir un tableau JSON.");
            }

            List<CaptainCoasterCoasterSnapshot> items = new();
            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                Dictionary<string, string> rawAttributes = ReadStringDictionary(element, "rawAttributes");
                CaptainCoasterCoasterSnapshot item = new()
                {
                    SyncSessionId = sessionId,
                    ExternalSource = ReadString(element, "externalSource") ?? SourceName,
                    CaptainCoasterId = ReadString(element, "externalId") ?? string.Empty,
                    Name = ReadString(element, "name") ?? string.Empty,
                    Slug = ReadString(element, "slug"),
                    SourceUrl = ReadString(element, "sourceUrl"),
                    ParkName = ReadString(element, "parkName"),
                    ParkSlug = ReadString(element, "parkSlug"),
                    ParkCaptainCoasterId = TryExtractParkExternalIdFromSlug(ReadString(element, "parkSlug")),
                    Country = ReadString(element, "country"),
                    Manufacturer = ReadString(element, "manufacturer"),
                    Model = ReadString(element, "model"),
                    MaterialType = ReadString(element, "materialType"),
                    SeatingType = ReadString(element, "seatingType"),
                    LaunchType = ReadString(element, "launchType"),
                    RestraintType = ReadString(element, "restraintType"),
                    IsLaunched = ReadBool(element, "isLaunched"),
                    HeightInFeet = ReadDouble(element, "heightInFeet"),
                    HeightInMeters = ReadDouble(element, "heightInMeters"),
                    LengthInFeet = ReadDouble(element, "lengthInFeet"),
                    LengthInMeters = ReadDouble(element, "lengthInMeters"),
                    SpeedInMph = ReadDouble(element, "speedInMph"),
                    SpeedInKmH = ReadDouble(element, "speedInKmH"),
                    InversionCount = ReadInt(element, "inversionCount"),
                    Status = ReadString(element, "status"),
                    OpeningDateText = ReadString(element, "openingDateText"),
                    ClosingDateText = ReadString(element, "closingDateText"),
                    OpeningDate = ParseFlexibleDate(ReadString(element, "openingDateText")),
                    ClosingDate = ParseFlexibleDate(ReadString(element, "closingDateText")),
                    ScrapedAtUtc = ReadDateTime(element, "scrapedAtUtc"),
                    RawAttributes = rawAttributes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                EnrichCoasterMetricsFromRawAttributes(item, rawAttributes);
                PopulateImperialMetrics(item);

                if (string.IsNullOrWhiteSpace(item.CaptainCoasterId) || string.IsNullOrWhiteSpace(item.Name))
                {
                    continue;
                }

                items.Add(item);
            }

            return items;
        }

        private async Task<CaptainCoasterDataSourceSettings> GetOrCreateSettingsAsync()
        {
            CaptainCoasterDataSourceSettings? settings = await settingsCollection.Find(item => item.SourceKey == SourceKey).FirstOrDefaultAsync();
            if (settings != null)
            {
                return settings;
            }

            settings = new CaptainCoasterDataSourceSettings
            {
                Source = SourceName,
                SourceKey = SourceKey,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
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
            await sessionsCollection.ReplaceOneAsync(item => item.Id == session.Id, session, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }

        private static void AddLog(CaptainCoasterSyncSession session, string level, string message)
        {
            session.Logs.Add(new CaptainCoasterSyncLogEntry
            {
                Level = level,
                Message = message,
                OccurredAtUtc = DateTime.UtcNow
            });

            if (session.Logs.Count > 200)
            {
                session.Logs = session.Logs.OrderByDescending(item => item.OccurredAtUtc).Take(200).OrderBy(item => item.OccurredAtUtc).ToList();
            }
        }

        private static Park? MatchPark(IEnumerable<Park> localParks, CaptainCoasterParkSnapshot externalPark)
        {
            return MatchPark(localParks, externalPark.Name, externalPark.CountryCode ?? externalPark.CountryRaw);
        }

        private static Park? MatchPark(IEnumerable<Park> localParks, string? parkName, string? country)
        {
            if (string.IsNullOrWhiteSpace(parkName))
            {
                return null;
            }

            string normalizedName = Normalize(parkName);
            string normalizedCountry = Normalize(country);

            Park? exact = localParks.FirstOrDefault(item => Normalize(item.Name) == normalizedName && Normalize(item.CountryCode) == normalizedCountry);
            if (exact != null)
            {
                return exact;
            }

            return localParks.FirstOrDefault(item => Normalize(item.Name) == normalizedName);
        }

        private static ParkItem? MatchCoaster(
            IEnumerable<ParkItem> localCoasters,
            IReadOnlyDictionary<string, AttractionManufacturer> manufacturersById,
            Park? matchedPark,
            CaptainCoasterCoasterSnapshot externalCoaster)
        {
            ParkItem? byExternalId = localCoasters.FirstOrDefault(item =>
                string.Equals(item.AttractionDetails?.ExternalSource, externalCoaster.ExternalSource, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.AttractionDetails?.ExternalId, externalCoaster.CaptainCoasterId, StringComparison.OrdinalIgnoreCase));
            if (byExternalId != null)
            {
                return byExternalId;
            }

            IEnumerable<ParkItem> candidates = localCoasters.Where(item => Normalize(item.Name) == Normalize(externalCoaster.Name));
            if (matchedPark != null)
            {
                ParkItem? byParkAndName = candidates.FirstOrDefault(item => string.Equals(item.ParkId, matchedPark.Id, StringComparison.Ordinal));
                if (byParkAndName != null)
                {
                    return byParkAndName;
                }
            }

            string normalizedManufacturer = Normalize(externalCoaster.Manufacturer);
            return candidates.FirstOrDefault(item => Normalize(ResolveManufacturerName(item.AttractionDetails?.ManufacturerId, manufacturersById)) == normalizedManufacturer)
                ?? candidates.FirstOrDefault();
        }

        private static string GetCoasterMatchConfidence(ParkItem? localCoaster, CaptainCoasterCoasterSnapshot externalCoaster)
        {
            if (localCoaster == null)
            {
                return "None";
            }

            if (string.Equals(localCoaster.AttractionDetails?.ExternalId, externalCoaster.CaptainCoasterId, StringComparison.OrdinalIgnoreCase))
            {
                return "High";
            }

            if (string.Equals(localCoaster.Name, externalCoaster.Name, StringComparison.OrdinalIgnoreCase))
            {
                return "Medium";
            }

            return "Low";
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

        private static void AddChange(List<CaptainCoasterFieldChange> changes, string field, string? localValue, string? externalValue)
        {
            string? normalizedLocal = NormalizeValue(localValue);
            string? normalizedExternal = NormalizeValue(externalValue);
            changes.Add(new CaptainCoasterFieldChange
            {
                Field = field,
                LocalValue = localValue,
                ExternalValue = externalValue,
                IsDifferent = !string.Equals(normalizedLocal, normalizedExternal, StringComparison.OrdinalIgnoreCase)
            });
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new(value.Length);
            foreach (char character in value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD))
            {
                if (char.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        private static string? NormalizeValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeFileName(string fileName)
        {
            return Path.GetFileName(fileName).Trim().ToLowerInvariant();
        }

        private static string? ResolveCountryCode(string? rawValue, IReadOnlyDictionary<string, string> countryCodeMap)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            if (countryCodeMap.TryGetValue(rawValue.Trim(), out string? direct))
            {
                return direct;
            }

            return CountryCodeMapper.ToCountryCode(rawValue);
        }

        private static string BuildManifestSummary(IReadOnlyDictionary<string, CaptainCoasterImportedFile> files)
        {
            if (!files.TryGetValue("manifest.json", out CaptainCoasterImportedFile? manifestFile))
            {
                return $"{files.Count} fichier(s) importé(s).";
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(manifestFile.Content);
                JsonElement root = document.RootElement;
                int parks = ReadInt(root, "exportedParkCount") ?? 0;
                int coasters = ReadInt(root, "exportedCoasterCount") ?? 0;
                bool offline = ReadBool(root, "usedOfflineRawInput") == true;
                return $"Manifest : {parks} parc(s), {coasters} coaster(s), mode offline = {(offline ? "oui" : "non")}.";
            }
            catch
            {
                return $"{files.Count} fichier(s) importé(s).";
            }
        }

        private static string? TryExtractParkExternalIdFromSlug(string? parkSlug)
        {
            return string.IsNullOrWhiteSpace(parkSlug) ? null : null;
        }

        private static void EnrichCoasterMetricsFromRawAttributes(CaptainCoasterCoasterSnapshot item, IReadOnlyDictionary<string, string> rawAttributes)
        {
            if (!rawAttributes.TryGetValue("topMetrics", out string? topMetrics) || string.IsNullOrWhiteSpace(topMetrics))
            {
                return;
            }

            string[] parts = topMetrics.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && item.HeightInMeters == null)
            {
                item.HeightInMeters = ParseMetricValue(parts[0], "m");
            }

            if (parts.Length > 1 && item.SpeedInKmH == null)
            {
                item.SpeedInKmH = ParseMetricValue(parts[1], "km/h");
            }

            if (parts.Length > 2 && item.LengthInMeters == null)
            {
                item.LengthInMeters = ParseMetricValue(parts[2], "m");
            }

            if (parts.Length > 3 && item.InversionCount == null)
            {
                item.InversionCount = ParseMetricInt(parts[3]);
            }
        }

        private static void PopulateImperialMetrics(CaptainCoasterCoasterSnapshot item)
        {
            item.HeightInFeet ??= ConvertMetersToFeet(item.HeightInMeters);
            item.LengthInFeet ??= ConvertMetersToFeet(item.LengthInMeters);
            item.SpeedInMph ??= ConvertKmHToMph(item.SpeedInKmH);
        }

        private static double? ParseMetricValue(string? rawValue, string expectedUnit)
        {
            if (string.IsNullOrWhiteSpace(rawValue) || rawValue.Contains("<empty>", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string normalized = rawValue.Replace(expectedUnit, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            return double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out double value)
                ? value
                : null;
        }

        private static int? ParseMetricInt(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue) || rawValue.Contains("<empty>", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return int.TryParse(rawValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : null;
        }

        private static double? ConvertMetersToFeet(double? meters)
        {
            return meters == null ? null : Math.Round(meters.Value * 3.28084d, 2, MidpointRounding.AwayFromZero);
        }

        private static double? ConvertKmHToMph(double? kmh)
        {
            return kmh == null ? null : Math.Round(kmh.Value * 0.621371d, 2, MidpointRounding.AwayFromZero);
        }

        private static string? ReadString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value))
            {
                return null;
            }

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
            if (!element.TryGetProperty(propertyName, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
            {
                return parsed;
            }

            return null;
        }

        private static int? ReadInt(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }

            return null;
        }

        private static bool? ReadBool(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool parsed))
            {
                return parsed;
            }

            return null;
        }

        private static DateTime? ReadDateTime(JsonElement element, string propertyName)
        {
            string? value = ReadString(element, propertyName);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsed))
            {
                return parsed;
            }

            return null;
        }

        private static DateTime? ParseFlexibleDate(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            string value = rawValue.Trim();
            string[] exactFormats = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "yyyy/MM/dd" };
            if (DateTime.TryParseExact(value, exactFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime exact))
            {
                return exact.Date;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year) && year is >= 1800 and <= 3000)
            {
                return new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsed))
            {
                return parsed.Date;
            }

            return null;
        }

        private static List<string> ReadStringArray(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind != JsonValueKind.Array)
            {
                return new List<string>();
            }

            return value.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)
                .ToList();
        }

        private static Dictionary<string, string>? ReadStringDictionary(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            Dictionary<string, string> items = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty property in value.EnumerateObject())
            {
                items[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }

            return items.Count > 0 ? items : null;
        }

        private static string? FormatDouble(double? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture);
        }

        private static string? FormatDate(DateTime? value)
        {
            return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static string? FormatBool(bool? value)
        {
            return value?.ToString();
        }

        private static string? FormatInt(int? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture);
        }

        private static CaptainCoasterSettingsResponse MapSettings(CaptainCoasterDataSourceSettings settings)
        {
            return new CaptainCoasterSettingsResponse
            {
                SourceKey = settings.SourceKey,
                DisplayName = settings.DisplayName,
                Description = settings.Description,
                InputMode = settings.InputMode,
                IsEnabled = settings.IsEnabled,
                LastSuccessfulImportUtc = settings.LastSuccessfulImportUtc,
                ExpectedFiles = new[]
                {
                    "coasters.json",
                    "detected-parks.json",
                    "manifest.json",
                    "coaster-urls.json",
                    "errors.json",
                    "stats.txt"
                }
            };
        }

        private static CaptainCoasterSyncSessionResponse MapSession(CaptainCoasterSyncSession session)
        {
            return new CaptainCoasterSyncSessionResponse
            {
                Id = session.Id,
                SourceKey = session.SourceKey,
                Status = session.Status,
                ProgressPercentage = session.ProgressPercentage,
                CurrentStep = session.CurrentStep,
                Message = session.Message,
                StartedAtUtc = session.StartedAtUtc,
                CompletedAtUtc = session.CompletedAtUtc,
                SourceFileCount = session.SourceFileCount,
                SourceFileNames = session.SourceFileNames,
                ParksFetched = session.Metrics.ParksFetched,
                CoastersFetched = session.Metrics.CoastersFetched,
                ComparisonResults = session.Metrics.ComparisonResults,
                AppliedChanges = session.Metrics.AppliedChanges,
                ManifestSummary = session.ManifestSummary,
                Logs = session.Logs.Select(item => new CaptainCoasterSyncLogResponse
                {
                    OccurredAtUtc = item.OccurredAtUtc,
                    Level = item.Level,
                    Message = item.Message
                }).ToList()
            };
        }

        private static CaptainCoasterComparisonResultResponse MapComparison(CaptainCoasterComparisonResult item)
        {
            return new CaptainCoasterComparisonResultResponse
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
    }

    internal static class CountryCodeMapper
    {
        internal static readonly Dictionary<string, string> Values = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Belgium"] = "BE",
            ["France"] = "FR",
            ["Germany"] = "DE",
            ["United Kingdom"] = "GB",
            ["UK"] = "GB",
            ["USA"] = "US",
            ["United States"] = "US",
            ["Spain"] = "ES",
            ["Italy"] = "IT",
            ["Netherlands"] = "NL",
            ["Poland"] = "PL",
            ["Sweden"] = "SE",
            ["Denmark"] = "DK",
            ["Finland"] = "FI",
            ["Switzerland"] = "CH",
            ["Austria"] = "AT",
            ["Portugal"] = "PT",
            ["Japan"] = "JP",
            ["Canada"] = "CA",
            ["Brazil"] = "BR"
        };

        public static string? ToCountryCode(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            if (Values.TryGetValue(rawValue.Trim(), out string? code))
            {
                return code;
            }

            if (rawValue.Length == 2)
            {
                return rawValue.ToUpperInvariant();
            }

            return null;
        }
    }
}
