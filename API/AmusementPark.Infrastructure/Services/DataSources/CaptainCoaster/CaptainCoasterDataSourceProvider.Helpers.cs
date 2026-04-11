using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AmusementPark.Application.Features.DataSources.Results;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Services.DataSources.Acquisition;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;

internal sealed partial class CaptainCoasterDataSourceProvider : IDataSourceProvider, IDataSourceImportExecutor
{
        // -----------------------------------------------------------------------
        // Session helpers
        // -----------------------------------------------------------------------

        private async Task<CaptainCoasterSettingsDocument> GetOrCreateSettingsAsync()
        {
            CaptainCoasterSettingsDocument? settings = await settingsCollection.Find(item => item.Source == LegacyExternalSourceValue).FirstOrDefaultAsync();
            if (settings != null) { return settings; }
            settings = new CaptainCoasterSettingsDocument { Source = LegacyExternalSourceValue, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            await settingsCollection.InsertOneAsync(settings);
            return settings;
        }

        private async Task UpdateSessionAsync(CaptainCoasterSyncSessionDocument session, string status, string message, int progressPercentage, CancellationToken cancellationToken)
        {
            session.Status = status;
            session.CurrentStep = status;
            session.Message = message;
            session.ProgressPercentage = progressPercentage;
            session.UpdatedAt = DateTime.UtcNow;
            AddLog(session, "Info", message);
            await PersistSessionAsync(session, cancellationToken);
        }

        private async Task PersistSessionAsync(CaptainCoasterSyncSessionDocument session, CancellationToken cancellationToken)
        {
            UpdateDefinition<CaptainCoasterSyncSessionDocument> update = Builders<CaptainCoasterSyncSessionDocument>.Update
                .Set(item => item.Id, session.Id)
                .Set(item => item.SourceKey, session.SourceKey)
                .Set(item => item.Status, session.Status)
                .Set(item => item.StartedAtUtc, session.StartedAtUtc)
                .Set(item => item.CompletedAtUtc, session.CompletedAtUtc)
                .Set(item => item.ProgressPercentage, session.ProgressPercentage)
                .Set(item => item.CurrentStep, session.CurrentStep)
                .Set(item => item.Message, session.Message)
                .Set(item => item.ImportKind, session.ImportKind)
                .Set(item => item.LastCompletedStep, session.LastCompletedStep)
                .Set(item => item.AvailableSteps, session.AvailableSteps)
                .Set(item => item.CanResume, session.CanResume)
                .Set(item => item.Metrics, session.Metrics)
                .Set(item => item.Logs, session.Logs)
                .Set(item => item.CreatedAt, session.CreatedAt)
                .Set(item => item.UpdatedAt, session.UpdatedAt)
                .Unset(item => item.DiscoveredUrls);

            await this.sessionsCollection.UpdateOneAsync(
                item => item.Id == session.Id,
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }

        private static DataAcquisitionRequestOptions BuildRequestOptions(CaptainCoasterScrapingSettings scrapingSettings)
        {
            return new DataAcquisitionRequestOptions
            {
                DelayBetweenRequestsMs = scrapingSettings.DelayBetweenRequestsMs,
                TimeoutSeconds = scrapingSettings.TimeoutSeconds,
                MaxRetryCount = scrapingSettings.MaxRetryCount,
            };
        }

        private static int NormalizePositiveBounded(int value, int fallback, int minValue, int maxValue)
        {
            int effective = value <= 0 ? fallback : value;
            return Math.Clamp(effective, minValue, maxValue);
        }

        private static List<List<TItem>> ChunkItems<TItem>(IReadOnlyCollection<TItem> items, int chunkSize)
        {
            List<List<TItem>> result = new List<List<TItem>>();
            if (items.Count == 0)
            {
                return result;
            }

            int effectiveChunkSize = Math.Max(1, chunkSize);
            List<TItem> current = new List<TItem>(effectiveChunkSize);
            foreach (TItem item in items)
            {
                current.Add(item);
                if (current.Count >= effectiveChunkSize)
                {
                    result.Add(current);
                    current = new List<TItem>(effectiveChunkSize);
                }
            }

            if (current.Count > 0)
            {
                result.Add(current);
            }

            return result;
        }

        private static void AddLog(CaptainCoasterSyncSessionDocument session, string level, string message)
        {
            session.Logs.Add(new CaptainCoasterSyncLogEntryDocument { Level = level, Message = message, OccurredAtUtc = DateTime.UtcNow });
            if (session.Logs.Count > 200)
            {
                session.Logs = session.Logs.OrderByDescending(item => item.OccurredAtUtc).Take(200).OrderBy(item => item.OccurredAtUtc).ToList();
            }
        }

        private static void AddChange(List<CaptainCoasterFieldChangeDocument> changes, string field, string? localValue, string? externalValue)
        {
            string? normalizedLocal = string.IsNullOrWhiteSpace(localValue) ? null : localValue.Trim();
            string? normalizedExternal = string.IsNullOrWhiteSpace(externalValue) ? null : externalValue.Trim();
            changes.Add(new CaptainCoasterFieldChangeDocument
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


    private static DataSourceSessionResult MapSession(CaptainCoasterSyncSessionDocument session)
    {
        return new DataSourceSessionResult
        {
            SessionId = session.Id,
            SourceKey = SourceKeyValue,
            Status = session.Status,
            ImportKind = session.ImportKind,
            ProgressPercentage = session.ProgressPercentage,
            CurrentStep = session.CurrentStep,
            LastCompletedStep = session.LastCompletedStep,
            Message = session.Message,
            CanResume = session.CanResume,
            AvailableSteps = session.AvailableSteps,
            StartedAtUtc = session.StartedAtUtc,
            CompletedAtUtc = session.CompletedAtUtc,
            Metrics = new DataSourceMetricsResult
            {
                ItemsFetchedPrimary = session.Metrics.ParksFetched,
                ItemsFetchedSecondary = session.Metrics.CoastersFetched,
                ComparisonResults = session.Metrics.ComparisonResults,
                AppliedChanges = session.Metrics.AppliedChanges,
                DuplicateConflicts = session.Metrics.DuplicateConflicts,
                DiscoveredItems = session.Metrics.DiscoveredItems,
                ProcessedItems = session.Metrics.ProcessedItems,
                FailedItems = session.Metrics.FailedItems,
                SkippedItems = session.Metrics.SkippedItems,
            },
            Logs = session.Logs.Select(item => new DataSourceLogResult
            {
                OccurredAtUtc = item.OccurredAtUtc,
                Level = item.Level,
                Message = item.Message,
            }).ToList(),
        };
    }

    private static DataSourceComparisonItemResult MapComparison(CaptainCoasterComparisonResultDocument item)
    {
        return new DataSourceComparisonItemResult
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
            Changes = item.Changes.Select(change => new DataSourceComparisonFieldChangeResult
            {
                Field = change.Field,
                LocalValue = change.LocalValue,
                ExternalValue = change.ExternalValue,
                IsDifferent = change.IsDifferent,
            }).ToList(),
            ExternalVariants = item.ExternalVariants.Select(variant => new DataSourceComparisonVariantResult
            {
                ExternalVariantId = variant.ExternalVariantId,
                DisplayLabel = variant.DisplayLabel,
                CandidateLocalEntityId = variant.CandidateLocalEntityId,
                SourceUrl = variant.SourceUrl,
                IsSuggested = variant.IsSuggested,
                Changes = variant.Changes.Select(change => new DataSourceComparisonFieldChangeResult
                {
                    Field = change.Field,
                    LocalValue = change.LocalValue,
                    ExternalValue = change.ExternalValue,
                    IsDifferent = change.IsDifferent,
                }).ToList(),
            }).ToList(),
        };
    }
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
