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

internal static class CaptainCoasterApplyWorkflow
{
    internal static async Task<CaptainCoasterApplyImpact> ApplyParkResultAsync(this CaptainCoasterDataSourceProvider provider,
                CaptainCoasterComparisonResultDocument result,
                DataSourceDuplicateResolution? resolution,
                CancellationToken cancellationToken)
    {
        CaptainCoasterParkSnapshotDocument? externalParkDocument = await provider.ResolveParkSnapshotAsync(result, resolution, cancellationToken);
        if (externalParkDocument == null)
        {
            return new CaptainCoasterApplyImpact { Applied = false };
        }

        List<ParkDocument> localParks = await provider.localParksCollection.Find(Builders<ParkDocument>.Filter.Empty).ToListAsync(cancellationToken);
        ParkDocument? localParkDocument = null;
        if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
        {
            localParkDocument = localParks.FirstOrDefault(item => item.Id == result.LocalEntityId);
        }
        localParkDocument ??= provider.MatchPark(localParks, externalParkDocument);

        DateTime utcNow = DateTime.UtcNow;
        if (localParkDocument == null)
        {
            localParkDocument = new ParkDocument
            {
                Name = externalParkDocument.Name,
                CountryCode = provider.NormalizeCountryCodeForStorage(externalParkDocument.CountryCode),
                Latitude = externalParkDocument.Latitude,
                Longitude = externalParkDocument.Longitude,
                IsVisible = false,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };
            localParkDocument.RefreshLocation();
            await provider.localParksCollection.InsertOneAsync(localParkDocument, cancellationToken: cancellationToken);
        }
        else
        {
            provider.ApplyExternalParkSnapshotToLocalPark(localParkDocument, externalParkDocument, utcNow);
            await provider.localParksCollection.ReplaceOneAsync(item => item.Id == localParkDocument.Id, localParkDocument, cancellationToken: cancellationToken);
        }

        result.IsApplied = true;
        result.LocalEntityId = localParkDocument.Id;
        result.AppliedExternalVariantId = externalParkDocument.Id;
        result.ResolutionStatus = result.RequiresManualResolution ? (resolution?.Strategy ?? "SelectVariant") : "Applied";
        result.UpdatedAt = DateTime.UtcNow;
        await provider.comparisonCollection.ReplaceOneAsync(item => item.Id == result.Id, result, cancellationToken: cancellationToken);
        return new CaptainCoasterApplyImpact
        {
            Applied = true,
            ParkId = localParkDocument.Id,
        };
    }

    internal static async Task<CaptainCoasterApplyImpact> ApplyCoasterResultAsync(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution? resolution,
        CancellationToken cancellationToken)
    {
        CaptainCoasterCoasterSnapshotDocument? externalCoaster = await provider.ResolveCoasterSnapshotAsync(result, resolution, cancellationToken);
        if (externalCoaster == null)
        {
            return new CaptainCoasterApplyImpact { Applied = false };
        }

        ParkDocument? park = await provider.ResolveOrCreateLocalParkForCoasterAsync(result.SyncSessionId, externalCoaster, cancellationToken);
        if (park == null)
        {
            return new CaptainCoasterApplyImpact { Applied = false };
        }

        AttractionManufacturerDocument? manufacturer = await provider.ResolveManufacturerAsync(externalCoaster.Manufacturer, cancellationToken);
        List<ParkItemDocument> localCoasters = await provider.localParkItemsCollection
            .Find(item => item.Category == ParkItemCategory.Attraction)
            .ToListAsync(cancellationToken);

        ParkItemDocument? localCoaster = provider.ResolveSelectedLocalCoasterForImport(result, externalCoaster, park.Id, localCoasters);
        localCoaster ??= provider.MatchCoasterInPark(localCoasters, park.Id, externalCoaster);

        AttractionDetailsDocument attractionDetails = localCoaster?.AttractionDetails ?? new AttractionDetailsDocument();
        attractionDetails.ManufacturerId = manufacturer?.Id;
        attractionDetails.Model = externalCoaster.Model;
        attractionDetails.ExternalSource = CaptainCoasterDataSourceProvider.LegacyExternalSourceValue;
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
        attractionDetails.HeightInFeet = provider.ConvertMetersToFeet(externalCoaster.HeightInMeters);
        attractionDetails.HeightInMeters = externalCoaster.HeightInMeters;
        attractionDetails.LengthInFeet = provider.ConvertMetersToFeet(externalCoaster.LengthInMeters);
        attractionDetails.LengthInMeters = externalCoaster.LengthInMeters;
        attractionDetails.SpeedInMph = provider.ConvertKmHToMph(externalCoaster.SpeedInKmH);
        attractionDetails.SpeedInKmH = externalCoaster.SpeedInKmH;
        attractionDetails.InversionCount = externalCoaster.InversionCount;

        if (localCoaster == null)
        {
            localCoaster = new ParkItemDocument
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
            await provider.localParkItemsCollection.InsertOneAsync(localCoaster, cancellationToken: cancellationToken);
        }
        else
        {
            localCoaster.Name = externalCoaster.Name;
            localCoaster.ParkId = park.Id;
            localCoaster.AttractionDetails = attractionDetails;
            localCoaster.UpdatedAt = DateTime.UtcNow;
            await provider.localParkItemsCollection.ReplaceOneAsync(item => item.Id == localCoaster.Id, localCoaster, cancellationToken: cancellationToken);
        }

        result.IsApplied = true;
        result.LocalEntityId = localCoaster.Id;
        result.AppliedExternalVariantId = externalCoaster.Id;
        result.ResolutionStatus = result.RequiresManualResolution ? (resolution?.Strategy ?? "SelectVariant") : "Applied";
        result.UpdatedAt = DateTime.UtcNow;
        await provider.comparisonCollection.ReplaceOneAsync(item => item.Id == result.Id, result, cancellationToken: cancellationToken);
        return new CaptainCoasterApplyImpact
        {
            Applied = true,
            ParkId = park.Id,
            ParkItemId = localCoaster.Id,
        };
    }

    internal static async Task<CaptainCoasterParkSnapshotDocument?> ResolveParkSnapshotAsync(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution? resolution,
        CancellationToken cancellationToken)
    {
        if (!result.RequiresManualResolution)
        {
            string? snapshotId = result.ExternalVariants.FirstOrDefault()?.ExternalVariantId;
            if (!string.IsNullOrWhiteSpace(snapshotId))
            {
                return await provider.parksCollection.Find(item => item.Id == snapshotId).FirstOrDefaultAsync(cancellationToken);
            }

            return await provider.parksCollection
                .Find(item => item.CaptainCoasterId == result.ExternalEntityId && item.SyncSessionId == result.SyncSessionId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (resolution == null)
        {
            return null;
        }

        List<CaptainCoasterParkSnapshotDocument> parkVariants = await provider.parksCollection
            .Find(item => item.SyncSessionId == result.SyncSessionId && item.CaptainCoasterId == result.ExternalEntityId)
            .ToListAsync(cancellationToken);
        Dictionary<string, CaptainCoasterParkSnapshotDocument> variantsById = parkVariants
            .ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);

        if (variantsById.Count == 0)
        {
            return null;
        }

        if (string.Equals(resolution.Strategy, "Merge", StringComparison.OrdinalIgnoreCase))
        {
            ParkDocument? localParkDocument = null;
            if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
            {
                localParkDocument = await provider.localParksCollection.Find(item => item.Id == result.LocalEntityId).FirstOrDefaultAsync(cancellationToken);
            }
            return provider.BuildMergedParkSnapshot(result, resolution, variantsById, localParkDocument);
        }

        if (string.IsNullOrWhiteSpace(resolution.SelectedExternalVariantId))
        {
            return null;
        }

        variantsById.TryGetValue(resolution.SelectedExternalVariantId, out CaptainCoasterParkSnapshotDocument? selected);
        return selected;
    }

    internal static async Task<CaptainCoasterCoasterSnapshotDocument?> ResolveCoasterSnapshotAsync(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution? resolution,
        CancellationToken cancellationToken)
    {
        if (!result.RequiresManualResolution)
        {
            string? snapshotId = result.ExternalVariants.FirstOrDefault()?.ExternalVariantId;
            if (!string.IsNullOrWhiteSpace(snapshotId))
            {
                return await provider.coastersCollection.Find(item => item.Id == snapshotId).FirstOrDefaultAsync(cancellationToken);
            }

            return await provider.coastersCollection
                .Find(item => item.CaptainCoasterId == result.ExternalEntityId && item.SyncSessionId == result.SyncSessionId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (resolution == null)
        {
            return null;
        }

        List<CaptainCoasterCoasterSnapshotDocument> coasterVariants = await provider.coastersCollection
            .Find(item => item.SyncSessionId == result.SyncSessionId && item.CaptainCoasterId == result.ExternalEntityId)
            .ToListAsync(cancellationToken);
        Dictionary<string, CaptainCoasterCoasterSnapshotDocument> variantsById = coasterVariants
            .ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);

        if (variantsById.Count == 0)
        {
            return null;
        }

        if (string.Equals(resolution.Strategy, "Merge", StringComparison.OrdinalIgnoreCase))
        {
            ParkItemDocument? localCoaster = null;
            if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
            {
                localCoaster = await provider.localParkItemsCollection.Find(item => item.Id == result.LocalEntityId).FirstOrDefaultAsync(cancellationToken);
            }
            return provider.BuildMergedCoasterSnapshot(result, resolution, variantsById, localCoaster);
        }

        if (string.IsNullOrWhiteSpace(resolution.SelectedExternalVariantId))
        {
            return null;
        }

        variantsById.TryGetValue(resolution.SelectedExternalVariantId, out CaptainCoasterCoasterSnapshotDocument? selected);
        return selected;
    }

    internal static CaptainCoasterParkSnapshotDocument? BuildMergedParkSnapshot(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution resolution,
        IReadOnlyDictionary<string, CaptainCoasterParkSnapshotDocument> variantsById,
        ParkDocument? localParkDocument)
    {
        CaptainCoasterParkSnapshotDocument? baseVariant = provider.GetBaseParkVariant(result, resolution, variantsById);
        if (baseVariant == null)
        {
            return null;
        }

        CaptainCoasterParkSnapshotDocument merged = provider.CloneParkSnapshot(baseVariant);
        foreach (DataSourceFieldResolution fieldResolution in resolution.FieldResolutions)
        {
            provider.ApplyParkFieldResolution(merged, fieldResolution, variantsById, localParkDocument);
        }

        merged.Id = Guid.NewGuid().ToString();
        merged.CreatedAt = DateTime.UtcNow;
        merged.UpdatedAt = DateTime.UtcNow;
        return merged;
    }

    internal static CaptainCoasterCoasterSnapshotDocument? BuildMergedCoasterSnapshot(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution resolution,
        IReadOnlyDictionary<string, CaptainCoasterCoasterSnapshotDocument> variantsById,
        ParkItemDocument? localCoaster)
    {
        CaptainCoasterCoasterSnapshotDocument? baseVariant = provider.GetBaseCoasterVariant(result, resolution, variantsById);
        if (baseVariant == null)
        {
            return null;
        }

        CaptainCoasterCoasterSnapshotDocument merged = provider.CloneCoasterSnapshot(baseVariant);
        foreach (DataSourceFieldResolution fieldResolution in resolution.FieldResolutions)
        {
            provider.ApplyCoasterFieldResolution(merged, fieldResolution, variantsById, localCoaster);
        }

        merged.Id = Guid.NewGuid().ToString();
        merged.CreatedAt = DateTime.UtcNow;
        merged.UpdatedAt = DateTime.UtcNow;
        return merged;
    }

    internal static CaptainCoasterParkSnapshotDocument? GetBaseParkVariant(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution resolution,
        IReadOnlyDictionary<string, CaptainCoasterParkSnapshotDocument> variantsById)
    {
        string? candidateId = resolution.SelectedExternalVariantId
            ?? result.ExternalVariants.FirstOrDefault(item => item.IsSuggested)?.ExternalVariantId
            ?? result.ExternalVariants.FirstOrDefault()?.ExternalVariantId;
        if (string.IsNullOrWhiteSpace(candidateId))
        {
            return null;
        }

        variantsById.TryGetValue(candidateId, out CaptainCoasterParkSnapshotDocument? variant);
        return variant;
    }

    internal static CaptainCoasterCoasterSnapshotDocument? GetBaseCoasterVariant(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution resolution,
        IReadOnlyDictionary<string, CaptainCoasterCoasterSnapshotDocument> variantsById)
    {
        string? candidateId = resolution.SelectedExternalVariantId
            ?? result.ExternalVariants.FirstOrDefault(item => item.IsSuggested)?.ExternalVariantId
            ?? result.ExternalVariants.FirstOrDefault()?.ExternalVariantId;
        if (string.IsNullOrWhiteSpace(candidateId))
        {
            return null;
        }

        variantsById.TryGetValue(candidateId, out CaptainCoasterCoasterSnapshotDocument? variant);
        return variant;
    }

    internal static CaptainCoasterParkSnapshotDocument CloneParkSnapshot(this CaptainCoasterDataSourceProvider provider, CaptainCoasterParkSnapshotDocument source)
    {
        return new CaptainCoasterParkSnapshotDocument
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

    internal static CaptainCoasterCoasterSnapshotDocument CloneCoasterSnapshot(this CaptainCoasterDataSourceProvider provider, CaptainCoasterCoasterSnapshotDocument source)
    {
        return new CaptainCoasterCoasterSnapshotDocument
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

    internal static void ApplyParkFieldResolution(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterParkSnapshotDocument target,
        DataSourceFieldResolution fieldResolution,
        IReadOnlyDictionary<string, CaptainCoasterParkSnapshotDocument> variantsById,
        ParkDocument? localParkDocument)
    {
        if (string.Equals(fieldResolution.SourceType, "Local", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(fieldResolution.Field, "name", StringComparison.OrdinalIgnoreCase))
            {
                target.Name = localParkDocument?.Name ?? target.Name;
            }
            else if (string.Equals(fieldResolution.Field, "countryCode", StringComparison.OrdinalIgnoreCase))
            {
                target.CountryCode = localParkDocument?.CountryCode ?? target.CountryCode;
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(fieldResolution.ExternalVariantId))
        {
            return;
        }
        if (!variantsById.TryGetValue(fieldResolution.ExternalVariantId, out CaptainCoasterParkSnapshotDocument? source))
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

    internal static void ApplyCoasterFieldResolution(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterCoasterSnapshotDocument target,
        DataSourceFieldResolution fieldResolution,
        IReadOnlyDictionary<string, CaptainCoasterCoasterSnapshotDocument> variantsById,
        ParkItemDocument? localCoaster)
    {
        if (string.Equals(fieldResolution.SourceType, "Local", StringComparison.OrdinalIgnoreCase))
        {
            provider.ApplyLocalCoasterField(target, fieldResolution.Field, localCoaster);
            return;
        }

        if (string.IsNullOrWhiteSpace(fieldResolution.ExternalVariantId))
        {
            return;
        }
        if (!variantsById.TryGetValue(fieldResolution.ExternalVariantId, out CaptainCoasterCoasterSnapshotDocument? source))
        {
            return;
        }

        provider.ApplyExternalCoasterField(target, fieldResolution.Field, source);
    }

    internal static void ApplyLocalCoasterField(this CaptainCoasterDataSourceProvider provider, CaptainCoasterCoasterSnapshotDocument target, string field, ParkItemDocument? localCoaster)
    {
        AttractionDetailsDocument? details = localCoaster?.AttractionDetails;
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

    internal static void ApplyExternalCoasterField(this CaptainCoasterDataSourceProvider provider, CaptainCoasterCoasterSnapshotDocument target, string field, CaptainCoasterCoasterSnapshotDocument source)
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

    internal static ParkItemDocument? ResolveSelectedLocalCoasterForImport(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterComparisonResultDocument result,
        CaptainCoasterCoasterSnapshotDocument externalCoaster,
        string targetParkId,
        IEnumerable<ParkItemDocument> localCoasters)
    {
        if (string.IsNullOrWhiteSpace(result.LocalEntityId))
        {
            return null;
        }

        ParkItemDocument? selectedLocalCoaster = localCoasters
            .FirstOrDefault(item => string.Equals(item.Id, result.LocalEntityId.Trim(), StringComparison.Ordinal));

        if (selectedLocalCoaster == null)
        {
            return null;
        }

        return provider.IsSafeLocalCoasterImportMatch(selectedLocalCoaster, externalCoaster, targetParkId)
            ? selectedLocalCoaster
            : null;
    }

    internal static ParkDocument? MatchParkByCoasterContext(this CaptainCoasterDataSourceProvider provider,
        IEnumerable<ParkDocument> localParks,
        CaptainCoasterCoasterSnapshotDocument externalCoaster)
    {
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

    internal static async Task<ParkDocument?> ResolveOrCreateLocalParkForCoasterAsync(this CaptainCoasterDataSourceProvider provider,
        string sessionId,
        CaptainCoasterCoasterSnapshotDocument externalCoaster,
        CancellationToken cancellationToken)
    {
        List<ParkDocument> localParks = await provider.localParksCollection.Find(Builders<ParkDocument>.Filter.Empty).ToListAsync(cancellationToken);
        ParkDocument? localParkDocument = null;

        if (!string.IsNullOrWhiteSpace(externalCoaster.ParkCaptainCoasterId))
        {
            CaptainCoasterParkSnapshotDocument? externalParkDocument = await provider.parksCollection
                .Find(item => item.SyncSessionId == sessionId && item.CaptainCoasterId == externalCoaster.ParkCaptainCoasterId)
                .FirstOrDefaultAsync(cancellationToken);
            if (externalParkDocument != null)
            {
                localParkDocument = provider.MatchPark(localParks, externalParkDocument);
                if (localParkDocument == null)
                {
                    DateTime utcNow = DateTime.UtcNow;
                    localParkDocument = new ParkDocument
                    {
                        Name = externalParkDocument.Name,
                        CountryCode = provider.NormalizeCountryCodeForStorage(externalParkDocument.CountryCode),
                        Latitude = externalParkDocument.Latitude,
                        Longitude = externalParkDocument.Longitude,
                        IsVisible = false,
                        CreatedAt = utcNow,
                        UpdatedAt = utcNow
                    };
                    localParkDocument.RefreshLocation();
                    await provider.localParksCollection.InsertOneAsync(localParkDocument, cancellationToken: cancellationToken);
                }
            }
        }

        if (localParkDocument == null && !string.IsNullOrWhiteSpace(externalCoaster.ParkName))
        {
            localParkDocument = provider.MatchParkByCoasterContext(localParks, externalCoaster);
        }

        if (localParkDocument == null && !string.IsNullOrWhiteSpace(externalCoaster.ParkName))
        {
            DateTime utcNow = DateTime.UtcNow;
            localParkDocument = new ParkDocument
            {
                Name = externalCoaster.ParkName,
                CountryCode = provider.NormalizeCountryCodeForStorage(externalCoaster.CountryCode),
                Latitude = null,
                Longitude = null,
                IsVisible = false,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };
            localParkDocument.RefreshLocation();
            await provider.localParksCollection.InsertOneAsync(localParkDocument, cancellationToken: cancellationToken);
        }

        return localParkDocument;
    }

    internal static async Task<AttractionManufacturerDocument?> ResolveManufacturerAsync(this CaptainCoasterDataSourceProvider provider, string? manufacturerName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manufacturerName))
        {
            return null;
        }

        List<AttractionManufacturerDocument> manufacturers = await provider.manufacturersCollection.Find(Builders<AttractionManufacturerDocument>.Filter.Empty).ToListAsync(cancellationToken);
        AttractionManufacturerDocument? manufacturer = manufacturers.FirstOrDefault(item => provider.Normalize(item.Name) == provider.Normalize(manufacturerName));
        if (manufacturer != null)
        {
            return manufacturer;
        }

        manufacturer = new AttractionManufacturerDocument
        {
            Name = manufacturerName.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await provider.manufacturersCollection.InsertOneAsync(manufacturer, cancellationToken: cancellationToken);
        return manufacturer;
    }

    internal static int GetEntityApplyPriority(this CaptainCoasterDataSourceProvider provider, string entityType)
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
}
