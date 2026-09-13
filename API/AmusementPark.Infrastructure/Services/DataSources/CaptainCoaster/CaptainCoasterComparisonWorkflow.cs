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

internal static class CaptainCoasterComparisonWorkflow
{
    internal static async Task<List<CaptainCoasterComparisonResultDocument>> BuildComparisonResultsAsync(this CaptainCoasterDataSourceProvider provider,
                string sessionId,
                IReadOnlyCollection<CaptainCoasterParkSnapshotDocument> externalParks,
                IReadOnlyCollection<CaptainCoasterCoasterSnapshotDocument> externalCoasters,
                CancellationToken cancellationToken)
    {
        List<CaptainCoasterComparisonResultDocument> results = new List<CaptainCoasterComparisonResultDocument>();
        List<ParkDocument> localParks = await provider.localParksCollection.Find(Builders<ParkDocument>.Filter.Empty).ToListAsync(cancellationToken);
        List<ParkItemDocument> localCoasters = await provider.localParkItemsCollection.Find(item => item.Category == ParkItemCategory.Attraction).ToListAsync(cancellationToken);
        List<AttractionManufacturerDocument> manufacturers = await provider.manufacturersCollection.Find(Builders<AttractionManufacturerDocument>.Filter.Empty).ToListAsync(cancellationToken);
        Dictionary<string, AttractionManufacturerDocument> manufacturersById = manufacturers.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);

        IEnumerable<IGrouping<string, CaptainCoasterParkSnapshotDocument>> parkGroups = externalParks
            .GroupBy(item => item.CaptainCoasterId, StringComparer.Ordinal);
        foreach (IGrouping<string, CaptainCoasterParkSnapshotDocument> group in parkGroups)
        {
            List<CaptainCoasterParkSnapshotDocument> variants = group.ToList();
            if (variants.Count == 1)
            {
                CaptainCoasterParkSnapshotDocument externalParkDocument = variants[0];
                ParkDocument? localParkDocument = provider.MatchPark(localParks, externalParkDocument);
                CaptainCoasterComparisonResultDocument compResult = provider.BuildParkComparison(sessionId, localParkDocument, externalParkDocument);
                if (!string.Equals(compResult.ChangeType, "Identical", StringComparison.Ordinal))
                {
                    results.Add(compResult);
                }
            }
            else
            {
                results.Add(provider.BuildDuplicateParkComparison(sessionId, localParks, variants));
            }
        }

        IEnumerable<IGrouping<string, CaptainCoasterCoasterSnapshotDocument>> coasterGroups = externalCoasters
            .GroupBy(item => item.CaptainCoasterId, StringComparer.Ordinal);
        foreach (IGrouping<string, CaptainCoasterCoasterSnapshotDocument> group in coasterGroups)
        {
            List<CaptainCoasterCoasterSnapshotDocument> variants = group.ToList();
            if (variants.Count == 1)
            {
                CaptainCoasterCoasterSnapshotDocument externalCoaster = variants[0];
                ParkItemDocument? localCoaster = provider.MatchCoaster(localCoasters, localParks, externalParks, externalCoaster);
                CaptainCoasterComparisonResultDocument compResult = provider.BuildCoasterComparison(sessionId, localCoaster, externalCoaster, manufacturersById);
                if (!string.Equals(compResult.ChangeType, "Identical", StringComparison.Ordinal))
                {
                    results.Add(compResult);
                }
            }
            else
            {
                results.Add(provider.BuildDuplicateCoasterComparison(sessionId, localCoasters, localParks, externalParks, manufacturersById, variants));
            }
        }

        return results;
    }

    internal static CaptainCoasterComparisonResultDocument BuildParkComparison(this CaptainCoasterDataSourceProvider provider, string sessionId, ParkDocument? localParkDocument, CaptainCoasterParkSnapshotDocument externalParkDocument)
    {
        List<CaptainCoasterFieldChangeDocument> changes = provider.BuildParkChanges(localParkDocument, externalParkDocument);
        string changeType = localParkDocument == null ? "MissingLocal" : (changes.Any(item => item.IsDifferent) ? "Updated" : "Identical");
        string matchConfidence = localParkDocument == null ? "None" : "High";

        return new CaptainCoasterComparisonResultDocument
        {
            SourceKey = CaptainCoasterDataSourceProvider.SourceKeyValue,
            SyncSessionId = sessionId,
            EntityType = "Park",
            ChangeType = changeType,
            DisplayName = externalParkDocument.Name,
            LocalEntityId = localParkDocument?.Id,
            ExternalEntityId = externalParkDocument.CaptainCoasterId,
            MatchConfidence = matchConfidence,
            Changes = changes,
            HasExternalDuplicates = false,
            RequiresManualResolution = false,
            ResolutionStatus = "NotRequired",
            ExternalVariants = new List<CaptainCoasterExternalVariantOptionDocument>
                {
                    new CaptainCoasterExternalVariantOptionDocument
                    {
                        ExternalVariantId = externalParkDocument.Id,
                        DisplayLabel = provider.BuildParkVariantLabel(externalParkDocument),
                        CandidateLocalEntityId = localParkDocument?.Id,
                        SourceUrl = externalParkDocument.SourceUrl,
                        IsSuggested = true,
                        Changes = changes
                    }
                },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    internal static CaptainCoasterComparisonResultDocument BuildDuplicateParkComparison(this CaptainCoasterDataSourceProvider provider,
        string sessionId,
        IReadOnlyCollection<ParkDocument> localParks,
        IReadOnlyCollection<CaptainCoasterParkSnapshotDocument> variants)
    {
        List<CaptainCoasterExternalVariantOptionDocument> options = variants
            .Select(variant => provider.BuildParkVariantOption(localParks, variant))
            .ToList();
        provider.MarkSuggestedVariant(options);

        CaptainCoasterExternalVariantOptionDocument? suggested = options.FirstOrDefault(item => item.IsSuggested) ?? options.FirstOrDefault();
        List<CaptainCoasterFieldChangeDocument> summaryChanges = new List<CaptainCoasterFieldChangeDocument>();
        provider.AddChange(summaryChanges, "duplicateVariants", null, variants.Count.ToString(CultureInfo.InvariantCulture));

        return new CaptainCoasterComparisonResultDocument
        {
            SourceKey = CaptainCoasterDataSourceProvider.SourceKeyValue,
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

    internal static CaptainCoasterComparisonResultDocument BuildCoasterComparison(this CaptainCoasterDataSourceProvider provider, string sessionId, ParkItemDocument? localCoaster, CaptainCoasterCoasterSnapshotDocument externalCoaster, IReadOnlyDictionary<string, AttractionManufacturerDocument> manufacturersById)
    {
        List<CaptainCoasterFieldChangeDocument> changes = provider.BuildCoasterChanges(localCoaster, externalCoaster, manufacturersById);
        string changeType = localCoaster == null ? "MissingLocal" : (changes.Any(item => item.IsDifferent) ? "Updated" : "Identical");
        string matchConfidence = localCoaster == null ? "None" : "Medium";

        return new CaptainCoasterComparisonResultDocument
        {
            SourceKey = CaptainCoasterDataSourceProvider.SourceKeyValue,
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
            ExternalVariants = new List<CaptainCoasterExternalVariantOptionDocument>
                {
                    new CaptainCoasterExternalVariantOptionDocument
                    {
                        ExternalVariantId = externalCoaster.Id,
                        DisplayLabel = provider.BuildCoasterVariantLabel(externalCoaster),
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

    internal static CaptainCoasterComparisonResultDocument BuildDuplicateCoasterComparison(this CaptainCoasterDataSourceProvider provider,
        string sessionId,
        IReadOnlyCollection<ParkItemDocument> localCoasters,
        IReadOnlyCollection<ParkDocument> localParks,
        IReadOnlyCollection<CaptainCoasterParkSnapshotDocument> externalParks,
        IReadOnlyDictionary<string, AttractionManufacturerDocument> manufacturersById,
        IReadOnlyCollection<CaptainCoasterCoasterSnapshotDocument> variants)
    {
        List<CaptainCoasterExternalVariantOptionDocument> options = variants
            .Select(variant => provider.BuildCoasterVariantOption(localCoasters, localParks, externalParks, manufacturersById, variant))
            .ToList();
        provider.MarkSuggestedVariant(options);

        CaptainCoasterExternalVariantOptionDocument? suggested = options.FirstOrDefault(item => item.IsSuggested) ?? options.FirstOrDefault();
        List<CaptainCoasterFieldChangeDocument> summaryChanges = new List<CaptainCoasterFieldChangeDocument>();
        provider.AddChange(summaryChanges, "duplicateVariants", null, variants.Count.ToString(CultureInfo.InvariantCulture));

        return new CaptainCoasterComparisonResultDocument
        {
            SourceKey = CaptainCoasterDataSourceProvider.SourceKeyValue,
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

    internal static CaptainCoasterExternalVariantOptionDocument BuildParkVariantOption(this CaptainCoasterDataSourceProvider provider,
        IReadOnlyCollection<ParkDocument> localParks,
        CaptainCoasterParkSnapshotDocument variant)
    {
        ParkDocument? localParkDocument = provider.MatchPark(localParks, variant);
        return new CaptainCoasterExternalVariantOptionDocument
        {
            ExternalVariantId = variant.Id,
            DisplayLabel = provider.BuildParkVariantLabel(variant),
            CandidateLocalEntityId = localParkDocument?.Id,
            SourceUrl = variant.SourceUrl,
            Changes = provider.BuildParkChanges(localParkDocument, variant)
        };
    }

    internal static CaptainCoasterExternalVariantOptionDocument BuildCoasterVariantOption(this CaptainCoasterDataSourceProvider provider,
        IReadOnlyCollection<ParkItemDocument> localCoasters,
        IReadOnlyCollection<ParkDocument> localParks,
        IReadOnlyCollection<CaptainCoasterParkSnapshotDocument> externalParks,
        IReadOnlyDictionary<string, AttractionManufacturerDocument> manufacturersById,
        CaptainCoasterCoasterSnapshotDocument variant)
    {
        ParkItemDocument? localCoaster = provider.MatchCoaster(localCoasters, localParks, externalParks, variant);
        return new CaptainCoasterExternalVariantOptionDocument
        {
            ExternalVariantId = variant.Id,
            DisplayLabel = provider.BuildCoasterVariantLabel(variant),
            CandidateLocalEntityId = localCoaster?.Id,
            SourceUrl = variant.SourceUrl,
            Changes = provider.BuildCoasterChanges(localCoaster, variant, manufacturersById)
        };
    }

    internal static void MarkSuggestedVariant(this CaptainCoasterDataSourceProvider provider, List<CaptainCoasterExternalVariantOptionDocument> options)
    {
        if (options.Count == 0)
        {
            return;
        }

        List<CaptainCoasterExternalVariantOptionDocument> matchingLocal = options
            .Where(item => !string.IsNullOrWhiteSpace(item.CandidateLocalEntityId))
            .ToList();

        CaptainCoasterExternalVariantOptionDocument? suggested = null;
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

    internal static List<CaptainCoasterFieldChangeDocument> BuildParkChanges(this CaptainCoasterDataSourceProvider provider, ParkDocument? localParkDocument, CaptainCoasterParkSnapshotDocument externalParkDocument)
    {
        List<CaptainCoasterFieldChangeDocument> changes = new List<CaptainCoasterFieldChangeDocument>();
        provider.AddChange(changes, "name", localParkDocument?.Name, externalParkDocument.Name);
        provider.AddChange(changes, "countryCode", localParkDocument?.CountryCode, externalParkDocument.CountryCode);
        return changes;
    }

    internal static List<CaptainCoasterFieldChangeDocument> BuildCoasterChanges(this CaptainCoasterDataSourceProvider provider, ParkItemDocument? localCoaster, CaptainCoasterCoasterSnapshotDocument externalCoaster, IReadOnlyDictionary<string, AttractionManufacturerDocument> manufacturersById)
    {
        List<CaptainCoasterFieldChangeDocument> changes = new List<CaptainCoasterFieldChangeDocument>();
        provider.AddChange(changes, "name", localCoaster?.Name, externalCoaster.Name);
        string? localManufacturerName = provider.ResolveManufacturerName(localCoaster?.AttractionDetails?.ManufacturerId, manufacturersById);
        provider.AddChange(changes, "manufacturer", localManufacturerName, externalCoaster.Manufacturer);
        provider.AddChange(changes, "model", localCoaster?.AttractionDetails?.Model, externalCoaster.Model);
        provider.AddChange(changes, "externalSource", localCoaster?.AttractionDetails?.ExternalSource, CaptainCoasterDataSourceProvider.LegacyExternalSourceValue);
        provider.AddChange(changes, "externalId", localCoaster?.AttractionDetails?.ExternalId, externalCoaster.CaptainCoasterId);
        provider.AddChange(changes, "sourceUrl", localCoaster?.AttractionDetails?.SourceUrl, externalCoaster.SourceUrl);
        provider.AddChange(changes, "status", localCoaster?.AttractionDetails?.Status, externalCoaster.Status);
        provider.AddChange(changes, "materialType", localCoaster?.AttractionDetails?.MaterialType, externalCoaster.MaterialType);
        provider.AddChange(changes, "seatingType", localCoaster?.AttractionDetails?.SeatingType, externalCoaster.SeatingType);
        provider.AddChange(changes, "launchType", localCoaster?.AttractionDetails?.LaunchType, externalCoaster.LaunchType);
        provider.AddChange(changes, "restraintType", localCoaster?.AttractionDetails?.RestraintType, externalCoaster.Restraint);
        provider.AddChange(changes, "isLaunched", provider.FormatBool(localCoaster?.AttractionDetails?.IsLaunched), provider.FormatBool(externalCoaster.IsLaunched));
        provider.AddChange(changes, "openingDate", provider.FormatDate(localCoaster?.AttractionDetails?.OpeningDate), provider.FormatDate(externalCoaster.OpeningDate));
        provider.AddChange(changes, "closingDate", provider.FormatDate(localCoaster?.AttractionDetails?.ClosingDate), provider.FormatDate(externalCoaster.ClosingDate));
        provider.AddChange(changes, "heightInFeet", provider.FormatDouble(localCoaster?.AttractionDetails?.HeightInFeet), provider.FormatDouble(provider.ConvertMetersToFeet(externalCoaster.HeightInMeters)));
        provider.AddChange(changes, "heightInMeters", provider.FormatDouble(localCoaster?.AttractionDetails?.HeightInMeters), provider.FormatDouble(externalCoaster.HeightInMeters));
        provider.AddChange(changes, "lengthInFeet", provider.FormatDouble(localCoaster?.AttractionDetails?.LengthInFeet), provider.FormatDouble(provider.ConvertMetersToFeet(externalCoaster.LengthInMeters)));
        provider.AddChange(changes, "lengthInMeters", provider.FormatDouble(localCoaster?.AttractionDetails?.LengthInMeters), provider.FormatDouble(externalCoaster.LengthInMeters));
        provider.AddChange(changes, "speedInMph", provider.FormatDouble(localCoaster?.AttractionDetails?.SpeedInMph), provider.FormatDouble(provider.ConvertKmHToMph(externalCoaster.SpeedInKmH)));
        provider.AddChange(changes, "speedInKmH", provider.FormatDouble(localCoaster?.AttractionDetails?.SpeedInKmH), provider.FormatDouble(externalCoaster.SpeedInKmH));
        provider.AddChange(changes, "inversionCount", localCoaster?.AttractionDetails?.InversionCount?.ToString(CultureInfo.InvariantCulture), externalCoaster.InversionCount?.ToString(CultureInfo.InvariantCulture));
        return changes;
    }

    internal static string BuildParkVariantLabel(this CaptainCoasterDataSourceProvider provider, CaptainCoasterParkSnapshotDocument externalParkDocument)
    {
        string country = string.IsNullOrWhiteSpace(externalParkDocument.CountryCode) ? externalParkDocument.CountryRaw ?? "?" : externalParkDocument.CountryCode;
        return $"{externalParkDocument.Name} — {country}";
    }

    internal static string? ResolveManufacturerName(this CaptainCoasterDataSourceProvider provider, string? manufacturerId, IReadOnlyDictionary<string, AttractionManufacturerDocument> manufacturersById)
    {
        if (string.IsNullOrWhiteSpace(manufacturerId))
        {
            return null;
        }

        return manufacturersById.TryGetValue(manufacturerId, out AttractionManufacturerDocument? manufacturer)
            ? manufacturer.Name
            : manufacturerId;
    }

    internal static string BuildCoasterVariantLabel(this CaptainCoasterDataSourceProvider provider, CaptainCoasterCoasterSnapshotDocument externalCoaster)
    {
        string parkName = string.IsNullOrWhiteSpace(externalCoaster.ParkName) ? "Parc inconnu" : externalCoaster.ParkName;
        string manufacturer = string.IsNullOrWhiteSpace(externalCoaster.Manufacturer) ? "Constructeur inconnu" : externalCoaster.Manufacturer;
        return $"{externalCoaster.Name} — {parkName} — {manufacturer}";
    }

    internal static ParkDocument? MatchPark(this CaptainCoasterDataSourceProvider provider, IEnumerable<ParkDocument> localParks, CaptainCoasterParkSnapshotDocument externalParkDocument)
    {
        string normalizedName = provider.Normalize(externalParkDocument.Name);
        string normalizedCountryCode = provider.Normalize(externalParkDocument.CountryCode);

        List<ParkDocument> sameNameParks = localParks
            .Where(item => provider.Normalize(item.Name) == normalizedName)
            .ToList();

        if (!string.IsNullOrWhiteSpace(normalizedCountryCode))
        {
            ParkDocument? sameCountryPark = sameNameParks
                .FirstOrDefault(item => provider.Normalize(item.CountryCode) == normalizedCountryCode);
            if (sameCountryPark != null)
            {
                return sameCountryPark;
            }
        }

        return sameNameParks.Count == 1 ? sameNameParks[0] : null;
    }

    internal static ParkItemDocument? MatchCoaster(this CaptainCoasterDataSourceProvider provider,
        IEnumerable<ParkItemDocument> localCoasters,
        IEnumerable<ParkDocument> localParks,
        IEnumerable<CaptainCoasterParkSnapshotDocument> externalParks,
        CaptainCoasterCoasterSnapshotDocument externalCoaster)
    {
        ParkDocument? localPark = provider.ResolveLocalParkForCoaster(localParks, externalParks, externalCoaster);
        if (localPark == null)
        {
            return null;
        }

        return provider.MatchCoasterInPark(localCoasters, localPark.Id, externalCoaster);
    }

    internal static ParkDocument? ResolveLocalParkForCoaster(this CaptainCoasterDataSourceProvider provider,
        IEnumerable<ParkDocument> localParks,
        IEnumerable<CaptainCoasterParkSnapshotDocument> externalParks,
        CaptainCoasterCoasterSnapshotDocument externalCoaster)
    {
        if (!string.IsNullOrWhiteSpace(externalCoaster.ParkCaptainCoasterId))
        {
            CaptainCoasterParkSnapshotDocument? externalPark = externalParks.FirstOrDefault(item =>
                string.Equals(item.CaptainCoasterId, externalCoaster.ParkCaptainCoasterId, StringComparison.Ordinal));
            if (externalPark != null)
            {
                ParkDocument? localPark = provider.MatchPark(localParks, externalPark);
                if (localPark != null)
                {
                    return localPark;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(externalCoaster.ParkName))
        {
            return null;
        }

        string normalizedParkName = provider.Normalize(externalCoaster.ParkName);
        string normalizedCountryCode = provider.Normalize(externalCoaster.CountryCode);
        List<ParkDocument> sameNameParks = localParks
            .Where(item => provider.Normalize(item.Name) == normalizedParkName)
            .ToList();

        if (!string.IsNullOrWhiteSpace(normalizedCountryCode))
        {
            ParkDocument? sameCountryPark = sameNameParks
                .FirstOrDefault(item => provider.Normalize(item.CountryCode) == normalizedCountryCode);
            if (sameCountryPark != null)
            {
                return sameCountryPark;
            }
        }

        return sameNameParks.Count == 1 ? sameNameParks[0] : null;
    }

    internal static ParkItemDocument? MatchCoasterInPark(this CaptainCoasterDataSourceProvider provider,
        IEnumerable<ParkItemDocument> localCoasters,
        string localParkId,
        CaptainCoasterCoasterSnapshotDocument externalCoaster)
    {
        string normalizedExternalId = provider.Normalize(externalCoaster.CaptainCoasterId);
        string normalizedName = provider.Normalize(externalCoaster.Name);
        List<ParkItemDocument> sameParkCoasters = localCoasters
            .Where(item => string.Equals(item.ParkId, localParkId, StringComparison.Ordinal))
            .ToList();

        if (!string.IsNullOrWhiteSpace(normalizedExternalId))
        {
            ParkItemDocument? sameExternalIdCoaster = sameParkCoasters
                .FirstOrDefault(item => provider.IsCaptainCoasterLinkedTo(item, normalizedExternalId));
            if (sameExternalIdCoaster != null)
            {
                return sameExternalIdCoaster;
            }
        }

        return sameParkCoasters.FirstOrDefault(item =>
            provider.Normalize(item.Name) == normalizedName
            && provider.IsPotentialCaptainCoasterTarget(item));
    }

    internal static bool IsCaptainCoasterLinkedTo(this CaptainCoasterDataSourceProvider provider, ParkItemDocument localCoaster, string normalizedExternalId)
    {
        if (localCoaster.AttractionDetails == null)
        {
            return false;
        }

        return string.Equals(provider.Normalize(localCoaster.AttractionDetails.ExternalSource), provider.Normalize(CaptainCoasterDataSourceProvider.LegacyExternalSourceValue), StringComparison.Ordinal)
            && string.Equals(provider.Normalize(localCoaster.AttractionDetails.ExternalId), normalizedExternalId, StringComparison.Ordinal);
    }

    internal static bool IsPotentialCaptainCoasterTarget(this CaptainCoasterDataSourceProvider provider, ParkItemDocument localCoaster)
    {
        return localCoaster.Type == ParkItemType.RollerCoaster
            || string.Equals(provider.Normalize(localCoaster.AttractionDetails?.ExternalSource), provider.Normalize(CaptainCoasterDataSourceProvider.LegacyExternalSourceValue), StringComparison.Ordinal);
    }
}
