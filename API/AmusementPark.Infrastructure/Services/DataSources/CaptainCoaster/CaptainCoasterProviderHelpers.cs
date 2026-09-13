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

internal static class CaptainCoasterProviderHelpers
{
    internal static async Task<CaptainCoasterSettingsDocument> GetOrCreateSettingsAsync(this CaptainCoasterDataSourceProvider provider)
    {
        CaptainCoasterSettingsDocument? settings = await provider.settingsCollection.Find(item => item.Source == CaptainCoasterDataSourceProvider.LegacyExternalSourceValue).FirstOrDefaultAsync();
        if (settings != null) { return settings; }
        settings = new CaptainCoasterSettingsDocument { Source = CaptainCoasterDataSourceProvider.LegacyExternalSourceValue, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await provider.settingsCollection.InsertOneAsync(settings);
        return settings;
    }

    internal static async Task UpdateSessionAsync(this CaptainCoasterDataSourceProvider provider, CaptainCoasterSyncSessionDocument session, string status, string message, int progressPercentage, CancellationToken cancellationToken)
    {
        session.Status = status;
        session.CurrentStep = status;
        session.Message = message;
        session.ProgressPercentage = progressPercentage;
        session.UpdatedAt = DateTime.UtcNow;
        provider.AddLog(session, "Info", message);
        await provider.PersistSessionAsync(session, cancellationToken);
    }

    internal static async Task PersistSessionAsync(this CaptainCoasterDataSourceProvider provider, CaptainCoasterSyncSessionDocument session, CancellationToken cancellationToken)
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

        await provider.sessionsCollection.UpdateOneAsync(
            item => item.Id == session.Id,
            update,
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    internal static DataAcquisitionRequestOptions BuildRequestOptions(this CaptainCoasterDataSourceProvider provider, CaptainCoasterScrapingSettings scrapingSettings)
    {
        return new DataAcquisitionRequestOptions
        {
            DelayBetweenRequestsMs = scrapingSettings.DelayBetweenRequestsMs,
            TimeoutSeconds = scrapingSettings.TimeoutSeconds,
            MaxRetryCount = scrapingSettings.MaxRetryCount,
        };
    }

    internal static int NormalizePositiveBounded(this CaptainCoasterDataSourceProvider provider, int value, int fallback, int minValue, int maxValue)
    {
        int effective = value <= 0 ? fallback : value;
        return Math.Clamp(effective, minValue, maxValue);
    }

    internal static List<List<TItem>> ChunkItems<TItem>(this CaptainCoasterDataSourceProvider provider, IReadOnlyCollection<TItem> items, int chunkSize)
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

    internal static void AddLog(this CaptainCoasterDataSourceProvider provider, CaptainCoasterSyncSessionDocument session, string level, string message)
    {
        session.Logs.Add(new CaptainCoasterSyncLogEntryDocument { Level = level, Message = message, OccurredAtUtc = DateTime.UtcNow });
        if (session.Logs.Count > 200)
        {
            session.Logs = session.Logs.OrderByDescending(item => item.OccurredAtUtc).Take(200).OrderBy(item => item.OccurredAtUtc).ToList();
        }
    }

    internal static void AddChange(this CaptainCoasterDataSourceProvider provider, List<CaptainCoasterFieldChangeDocument> changes, string field, string? localValue, string? externalValue)
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

    internal static string? ReadString(this CaptainCoasterDataSourceProvider provider, JsonElement element, string propertyName)
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

    internal static double? ReadDouble(this CaptainCoasterDataSourceProvider provider, JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value)) { return null; }
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double r)) { return r; }
        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double p)) { return p; }
        return null;
    }

    internal static int? ReadInt(this CaptainCoasterDataSourceProvider provider, JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value)) { return null; }
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int r)) { return r; }
        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int p)) { return p; }
        return null;
    }

    internal static bool? ReadBool(this CaptainCoasterDataSourceProvider provider, JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value)) { return null; }
        if (value.ValueKind == JsonValueKind.True) { return true; }
        if (value.ValueKind == JsonValueKind.False) { return false; }
        if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool p)) { return p; }
        return null;
    }

    internal static DateTime? ReadDateTime(this CaptainCoasterDataSourceProvider provider, JsonElement element, string propertyName)
    {
        string? raw = provider.ReadString(element, propertyName);
        if (string.IsNullOrWhiteSpace(raw)) { return null; }
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime result)) { return result; }
        return null;
    }

    internal static List<string> ReadStringArray(this CaptainCoasterDataSourceProvider provider, JsonElement element, string propertyName)
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

    internal static async Task<byte[]> ReadStreamToBytesAsync(this CaptainCoasterDataSourceProvider provider, Stream stream, CancellationToken cancellationToken)
    {
        using MemoryStream ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }

    internal static string Normalize(this CaptainCoasterDataSourceProvider provider, string? value)
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

    internal static int CountDuplicateGroups(this CaptainCoasterDataSourceProvider provider, IEnumerable<string> values)
    {
        return values
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .GroupBy(item => item.Trim(), StringComparer.Ordinal)
            .Count(group => group.Count() > 1);
    }

    internal static string? NormalizeCountryCodeForStorage(this CaptainCoasterDataSourceProvider provider, string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return null;
        }

        return countryCode.Trim().ToUpperInvariant();
    }

    internal static void ApplyExternalParkSnapshotToLocalPark(this CaptainCoasterDataSourceProvider provider, ParkDocument localParkDocument, CaptainCoasterParkSnapshotDocument externalParkDocument, DateTime utcNow)
    {
        localParkDocument.Name = externalParkDocument.Name;

        string? normalizedCountryCode = provider.NormalizeCountryCodeForStorage(externalParkDocument.CountryCode);
        if (!string.IsNullOrWhiteSpace(normalizedCountryCode))
        {
            localParkDocument.CountryCode = normalizedCountryCode;
        }

        if (externalParkDocument.Latitude.HasValue && externalParkDocument.Longitude.HasValue)
        {
            localParkDocument.Latitude = externalParkDocument.Latitude;
            localParkDocument.Longitude = externalParkDocument.Longitude;
        }

        localParkDocument.UpdatedAt = utcNow;
        localParkDocument.RefreshLocation();
    }

    internal static double? ConvertMetersToFeet(this CaptainCoasterDataSourceProvider provider, double? value) => value == null ? null : Math.Round(value.Value * 3.28084d, 2, MidpointRounding.AwayFromZero);
    internal static double? ConvertKmHToMph(this CaptainCoasterDataSourceProvider provider, double? value) => value == null ? null : Math.Round(value.Value * 0.621371d, 2, MidpointRounding.AwayFromZero);
    internal static string? FormatDouble(this CaptainCoasterDataSourceProvider provider, double? value) => value?.ToString(CultureInfo.InvariantCulture);
    internal static string? FormatDate(this CaptainCoasterDataSourceProvider provider, DateTime? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    internal static string? FormatBool(this CaptainCoasterDataSourceProvider provider, bool? value) => value?.ToString();


    internal static DataSourceSessionResult MapSession(this CaptainCoasterDataSourceProvider provider, CaptainCoasterSyncSessionDocument session)
    {
        return new DataSourceSessionResult
        {
            SessionId = session.Id,
            SourceKey = CaptainCoasterDataSourceProvider.SourceKeyValue,
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

    internal static DataSourceComparisonItemResult MapComparison(this CaptainCoasterDataSourceProvider provider, CaptainCoasterComparisonResultDocument item)
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


    // ---------------------------------------------------------------------------
    // CountryNameMapper
    // ---------------------------------------------------------------------------


}
