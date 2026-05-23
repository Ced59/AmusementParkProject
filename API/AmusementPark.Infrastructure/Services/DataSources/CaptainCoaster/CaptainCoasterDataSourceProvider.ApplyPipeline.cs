using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;

internal sealed partial class CaptainCoasterDataSourceProvider : IDataSourceProvider, IDataSourceImportExecutor
{
    private async Task<CaptainCoasterApplyExecutionContext> BuildApplyExecutionContextAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        List<ParkDocument> localParks = await this.localParksCollection
            .Find(Builders<ParkDocument>.Filter.Empty)
            .ToListAsync(cancellationToken);

        List<ParkItemDocument> localCoasters = await this.localParkItemsCollection
            .Find(item => item.Category == ParkItemCategory.Attraction)
            .ToListAsync(cancellationToken);

        List<AttractionManufacturerDocument> manufacturers = await this.manufacturersCollection
            .Find(Builders<AttractionManufacturerDocument>.Filter.Empty)
            .ToListAsync(cancellationToken);

        List<CaptainCoasterParkSnapshotDocument> externalParks = await this.parksCollection
            .Find(item => item.SyncSessionId == sessionId)
            .ToListAsync(cancellationToken);

        List<CaptainCoasterCoasterSnapshotDocument> externalCoasters = await this.coastersCollection
            .Find(item => item.SyncSessionId == sessionId)
            .ToListAsync(cancellationToken);

        return new CaptainCoasterApplyExecutionContext(localParks, localCoasters, manufacturers, externalParks, externalCoasters);
    }

    private static bool HasPendingApplyWrites(CaptainCoasterApplyExecutionContext context, int batchSize)
    {
        return context.PendingParkWrites.Count >= batchSize
            || context.PendingParkItemWrites.Count >= batchSize
            || context.PendingManufacturerWrites.Count >= batchSize
            || context.PendingComparisonWrites.Count >= batchSize;
    }

    private async Task FlushApplyWritesAsync(
        CaptainCoasterApplyExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context.PendingParkWrites.Count > 0)
        {
            await this.localParksCollection.BulkWriteAsync(
                context.PendingParkWrites,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken);

            context.PendingParkWrites.Clear();
        }

        if (context.PendingParkItemWrites.Count > 0)
        {
            await this.localParkItemsCollection.BulkWriteAsync(
                context.PendingParkItemWrites,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken);

            context.PendingParkItemWrites.Clear();
        }

        if (context.PendingManufacturerWrites.Count > 0)
        {
            await this.manufacturersCollection.BulkWriteAsync(
                context.PendingManufacturerWrites,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken);

            context.PendingManufacturerWrites.Clear();
        }

        if (context.PendingComparisonWrites.Count > 0)
        {
            await this.comparisonCollection.BulkWriteAsync(
                context.PendingComparisonWrites,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken);

            context.PendingComparisonWrites.Clear();
        }
    }

    private CaptainCoasterApplyImpact ApplyParkResultWithContext(
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution? resolution,
        CaptainCoasterApplyExecutionContext context,
        DateTime utcNow)
    {
        CaptainCoasterParkSnapshotDocument? externalParkDocument = this.ResolveParkSnapshotWithContext(result, resolution, context);
        if (externalParkDocument == null)
        {
            return new CaptainCoasterApplyImpact { Applied = false };
        }

        ParkDocument? localParkDocument = null;
        if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
        {
            context.LocalParksById.TryGetValue(result.LocalEntityId.Trim(), out localParkDocument);
        }

        localParkDocument ??= FindMatchingPark(context, externalParkDocument);

        if (localParkDocument == null)
        {
            localParkDocument = new ParkDocument
            {
                Name = externalParkDocument.Name,
                CountryCode = NormalizeCountryCodeForStorage(externalParkDocument.CountryCode),
                Latitude = externalParkDocument.Latitude,
                Longitude = externalParkDocument.Longitude,
                IsVisible = false,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            };
            localParkDocument.RefreshLocation();
            context.LocalParks.Add(localParkDocument);
            context.LocalParksById[localParkDocument.Id] = localParkDocument;
            AddParkLookup(context, localParkDocument);
        }
        else
        {
            ApplyExternalParkSnapshotToLocalPark(localParkDocument, externalParkDocument, utcNow);
            AddParkLookup(context, localParkDocument);
        }

        context.PendingParkWrites.Add(
            new ReplaceOneModel<ParkDocument>(
                Builders<ParkDocument>.Filter.Eq(item => item.Id, localParkDocument.Id),
                localParkDocument)
            {
                IsUpsert = true,
            });

        result.IsApplied = true;
        result.LocalEntityId = localParkDocument.Id;
        result.AppliedExternalVariantId = externalParkDocument.Id;
        result.ResolutionStatus = result.RequiresManualResolution ? (resolution?.Strategy ?? "SelectVariant") : "Applied";
        result.UpdatedAt = utcNow;

        context.PendingComparisonWrites.Add(
            new ReplaceOneModel<CaptainCoasterComparisonResultDocument>(
                Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.Id, result.Id),
                result)
            {
                IsUpsert = false,
            });

        context.AffectedParkIds.Add(localParkDocument.Id);

        return new CaptainCoasterApplyImpact
        {
            Applied = true,
            ParkId = localParkDocument.Id,
        };
    }

    private CaptainCoasterApplyImpact ApplyCoasterResultWithContext(
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution? resolution,
        CaptainCoasterApplyExecutionContext context,
        DateTime utcNow)
    {
        CaptainCoasterCoasterSnapshotDocument? externalCoaster = this.ResolveCoasterSnapshotWithContext(result, resolution, context);
        if (externalCoaster == null)
        {
            return new CaptainCoasterApplyImpact { Applied = false };
        }

        ParkDocument? park = this.ResolveOrCreateLocalParkForCoasterWithContext(result.SyncSessionId, externalCoaster, context, utcNow);
        if (park == null)
        {
            return new CaptainCoasterApplyImpact { Applied = false };
        }

        AttractionManufacturerDocument? manufacturer = this.ResolveManufacturerWithContext(externalCoaster.Manufacturer, context, utcNow);

        ParkItemDocument? localCoaster = ResolveSelectedLocalCoasterForImport(result, externalCoaster, park.Id, context);
        localCoaster ??= FindMatchingLocalCoaster(context, externalCoaster, park.Id);

        AttractionDetailsDocument attractionDetails = localCoaster?.AttractionDetails ?? new AttractionDetailsDocument();
        attractionDetails.ManufacturerId = manufacturer?.Id;
        attractionDetails.Model = externalCoaster.Model;
        attractionDetails.ExternalSource = LegacyExternalSourceValue;
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
            localCoaster = new ParkItemDocument
            {
                ParkId = park.Id,
                Name = externalCoaster.Name,
                Category = ParkItemCategory.Attraction,
                Type = ParkItemType.RollerCoaster,
                IsVisible = false,
                AttractionDetails = attractionDetails,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            };
            context.LocalCoasters.Add(localCoaster);
            context.LocalCoastersById[localCoaster.Id] = localCoaster;
            AddCoasterLookup(context, localCoaster);
        }
        else
        {
            localCoaster.Name = externalCoaster.Name;
            localCoaster.ParkId = park.Id;
            localCoaster.AttractionDetails = attractionDetails;
            localCoaster.UpdatedAt = utcNow;
            AddCoasterLookup(context, localCoaster);
        }

        localCoaster.RefreshLocation();

        context.PendingParkItemWrites.Add(
            new ReplaceOneModel<ParkItemDocument>(
                Builders<ParkItemDocument>.Filter.Eq(item => item.Id, localCoaster.Id),
                localCoaster)
            {
                IsUpsert = true,
            });

        result.IsApplied = true;
        result.LocalEntityId = localCoaster.Id;
        result.AppliedExternalVariantId = externalCoaster.Id;
        result.ResolutionStatus = result.RequiresManualResolution ? (resolution?.Strategy ?? "SelectVariant") : "Applied";
        result.UpdatedAt = utcNow;

        context.PendingComparisonWrites.Add(
            new ReplaceOneModel<CaptainCoasterComparisonResultDocument>(
                Builders<CaptainCoasterComparisonResultDocument>.Filter.Eq(item => item.Id, result.Id),
                result)
            {
                IsUpsert = false,
            });

        context.AffectedParkIds.Add(park.Id);
        context.AffectedParkItemIds.Add(localCoaster.Id);

        return new CaptainCoasterApplyImpact
        {
            Applied = true,
            ParkId = park.Id,
            ParkItemId = localCoaster.Id,
        };
    }

    private CaptainCoasterParkSnapshotDocument? ResolveParkSnapshotWithContext(
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution? resolution,
        CaptainCoasterApplyExecutionContext context)
    {
        if (!result.RequiresManualResolution)
        {
            string? snapshotId = result.ExternalVariants.FirstOrDefault()?.ExternalVariantId;
            if (!string.IsNullOrWhiteSpace(snapshotId)
                && context.ParkSnapshotsById.TryGetValue(snapshotId.Trim(), out CaptainCoasterParkSnapshotDocument? selectedById))
            {
                return selectedById;
            }

            if (!string.IsNullOrWhiteSpace(result.ExternalEntityId)
                && context.ParkSnapshotsByCaptainCoasterId.TryGetValue(result.ExternalEntityId.Trim(), out List<CaptainCoasterParkSnapshotDocument>? variants))
            {
                return variants.FirstOrDefault();
            }

            return null;
        }

        if (resolution == null || string.IsNullOrWhiteSpace(result.ExternalEntityId))
        {
            return null;
        }

        List<CaptainCoasterParkSnapshotDocument> parkVariants = context.ParkSnapshotsByCaptainCoasterId.TryGetValue(
            result.ExternalEntityId.Trim(),
            out List<CaptainCoasterParkSnapshotDocument>? resolvedVariants)
            ? resolvedVariants
            : new List<CaptainCoasterParkSnapshotDocument>();

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
                context.LocalParksById.TryGetValue(result.LocalEntityId.Trim(), out localParkDocument);
            }

            return BuildMergedParkSnapshot(result, resolution, variantsById, localParkDocument);
        }

        if (string.IsNullOrWhiteSpace(resolution.SelectedExternalVariantId))
        {
            return null;
        }

        variantsById.TryGetValue(resolution.SelectedExternalVariantId.Trim(), out CaptainCoasterParkSnapshotDocument? selected);
        return selected;
    }

    private CaptainCoasterCoasterSnapshotDocument? ResolveCoasterSnapshotWithContext(
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution? resolution,
        CaptainCoasterApplyExecutionContext context)
    {
        if (!result.RequiresManualResolution)
        {
            string? snapshotId = result.ExternalVariants.FirstOrDefault()?.ExternalVariantId;
            if (!string.IsNullOrWhiteSpace(snapshotId)
                && context.CoasterSnapshotsById.TryGetValue(snapshotId.Trim(), out CaptainCoasterCoasterSnapshotDocument? selectedById))
            {
                return selectedById;
            }

            if (!string.IsNullOrWhiteSpace(result.ExternalEntityId)
                && context.CoasterSnapshotsByCaptainCoasterId.TryGetValue(result.ExternalEntityId.Trim(), out List<CaptainCoasterCoasterSnapshotDocument>? variants))
            {
                return variants.FirstOrDefault();
            }

            return null;
        }

        if (resolution == null || string.IsNullOrWhiteSpace(result.ExternalEntityId))
        {
            return null;
        }

        List<CaptainCoasterCoasterSnapshotDocument> coasterVariants = context.CoasterSnapshotsByCaptainCoasterId.TryGetValue(
            result.ExternalEntityId.Trim(),
            out List<CaptainCoasterCoasterSnapshotDocument>? resolvedVariants)
            ? resolvedVariants
            : new List<CaptainCoasterCoasterSnapshotDocument>();

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
                context.LocalCoastersById.TryGetValue(result.LocalEntityId.Trim(), out localCoaster);
            }

            return BuildMergedCoasterSnapshot(result, resolution, variantsById, localCoaster);
        }

        if (string.IsNullOrWhiteSpace(resolution.SelectedExternalVariantId))
        {
            return null;
        }

        variantsById.TryGetValue(resolution.SelectedExternalVariantId.Trim(), out CaptainCoasterCoasterSnapshotDocument? selected);
        return selected;
    }

    private static ParkDocument? FindMatchingPark(CaptainCoasterApplyExecutionContext context, CaptainCoasterParkSnapshotDocument externalParkDocument)
    {
        string normalizedName = Normalize(externalParkDocument.Name);
        string normalizedCountryCode = Normalize(externalParkDocument.CountryCode);
        string compositeKey = BuildParkCompositeKey(normalizedName, normalizedCountryCode);

        if (!string.IsNullOrWhiteSpace(normalizedCountryCode)
            && context.ParkIdsByNormalizedNameAndCountry.TryGetValue(compositeKey, out List<string>? idsByNameAndCountry))
        {
            foreach (string parkId in idsByNameAndCountry)
            {
                if (context.LocalParksById.TryGetValue(parkId, out ParkDocument? parkDocument))
                {
                    return parkDocument;
                }
            }
        }

        if (context.ParkIdsByNormalizedName.TryGetValue(normalizedName, out List<string>? idsByName)
            && idsByName.Count == 1
            && context.LocalParksById.TryGetValue(idsByName[0], out ParkDocument? singleNameMatch))
        {
            return singleNameMatch;
        }

        return null;
    }

    private static ParkItemDocument? ResolveSelectedLocalCoasterForImport(
        CaptainCoasterComparisonResultDocument result,
        CaptainCoasterCoasterSnapshotDocument externalCoaster,
        string targetParkId,
        CaptainCoasterApplyExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(result.LocalEntityId))
        {
            return null;
        }

        if (!context.LocalCoastersById.TryGetValue(result.LocalEntityId.Trim(), out ParkItemDocument? selectedLocalCoaster))
        {
            return null;
        }

        return IsSafeLocalCoasterImportMatch(selectedLocalCoaster, externalCoaster, targetParkId)
            ? selectedLocalCoaster
            : null;
    }

    private static bool IsSafeLocalCoasterImportMatch(
        ParkItemDocument localCoaster,
        CaptainCoasterCoasterSnapshotDocument externalCoaster,
        string targetParkId)
    {
        if (!string.Equals(localCoaster.ParkId, targetParkId, StringComparison.Ordinal))
        {
            return false;
        }

        string normalizedExternalId = Normalize(externalCoaster.CaptainCoasterId);
        if (!string.IsNullOrWhiteSpace(normalizedExternalId) && IsCaptainCoasterLinkedTo(localCoaster, normalizedExternalId))
        {
            return true;
        }

        return Normalize(localCoaster.Name) == Normalize(externalCoaster.Name)
            && IsPotentialCaptainCoasterTarget(localCoaster);
    }

    private static ParkItemDocument? FindMatchingLocalCoaster(
        CaptainCoasterApplyExecutionContext context,
        CaptainCoasterCoasterSnapshotDocument externalCoaster,
        string targetParkId)
    {
        return MatchCoasterInPark(context.LocalCoasters, targetParkId, externalCoaster);
    }

    private static string BuildParkCompositeKey(string normalizedName, string normalizedCountryCode)
    {
        return $"{normalizedName}|{normalizedCountryCode}";
    }

    private static string BuildCoasterCompositeKey(string normalizedName, string parkId)
    {
        return $"{normalizedName}|{parkId}";
    }

    private static void AddParkLookup(CaptainCoasterApplyExecutionContext context, ParkDocument parkDocument)
    {
        string normalizedName = Normalize(parkDocument.Name);
        string normalizedCountryCode = Normalize(parkDocument.CountryCode);
        string compositeKey = BuildParkCompositeKey(normalizedName, normalizedCountryCode);

        if (!context.ParkIdsByNormalizedName.TryGetValue(normalizedName, out List<string>? idsByName))
        {
            idsByName = new List<string>();
            context.ParkIdsByNormalizedName[normalizedName] = idsByName;
        }

        if (!idsByName.Any(item => string.Equals(item, parkDocument.Id, StringComparison.Ordinal)))
        {
            idsByName.Add(parkDocument.Id);
        }

        if (!context.ParkIdsByNormalizedNameAndCountry.TryGetValue(compositeKey, out List<string>? idsByNameAndCountry))
        {
            idsByNameAndCountry = new List<string>();
            context.ParkIdsByNormalizedNameAndCountry[compositeKey] = idsByNameAndCountry;
        }

        if (!idsByNameAndCountry.Any(item => string.Equals(item, parkDocument.Id, StringComparison.Ordinal)))
        {
            idsByNameAndCountry.Add(parkDocument.Id);
        }
    }

    private static ParkDocument? FindMatchingParkByCoasterContext(
        CaptainCoasterApplyExecutionContext context,
        CaptainCoasterCoasterSnapshotDocument externalCoaster)
    {
        if (string.IsNullOrWhiteSpace(externalCoaster.ParkName))
        {
            return null;
        }

        string normalizedParkName = Normalize(externalCoaster.ParkName);
        string normalizedCountryCode = Normalize(externalCoaster.CountryCode);

        if (!string.IsNullOrWhiteSpace(normalizedCountryCode))
        {
            string compositeKey = BuildParkCompositeKey(normalizedParkName, normalizedCountryCode);
            if (context.ParkIdsByNormalizedNameAndCountry.TryGetValue(compositeKey, out List<string>? candidateParkIdsByCountry))
            {
                foreach (string candidateParkId in candidateParkIdsByCountry)
                {
                    if (context.LocalParksById.TryGetValue(candidateParkId, out ParkDocument? candidatePark))
                    {
                        return candidatePark;
                    }
                }
            }
        }

        if (context.ParkIdsByNormalizedName.TryGetValue(normalizedParkName, out List<string>? candidateParkIds)
            && candidateParkIds.Count == 1
            && context.LocalParksById.TryGetValue(candidateParkIds[0], out ParkDocument? singleNameMatch))
        {
            return singleNameMatch;
        }

        return null;
    }

    private static void AddCoasterLookup(CaptainCoasterApplyExecutionContext context, ParkItemDocument parkItemDocument)
    {
        string normalizedName = Normalize(parkItemDocument.Name);
        string compositeKey = BuildCoasterCompositeKey(normalizedName, parkItemDocument.ParkId);

        if (!context.CoasterIdsByNormalizedName.TryGetValue(normalizedName, out List<string>? idsByName))
        {
            idsByName = new List<string>();
            context.CoasterIdsByNormalizedName[normalizedName] = idsByName;
        }

        if (!idsByName.Any(item => string.Equals(item, parkItemDocument.Id, StringComparison.Ordinal)))
        {
            idsByName.Add(parkItemDocument.Id);
        }

        if (!context.CoasterIdsByNormalizedNameAndParkId.TryGetValue(compositeKey, out List<string>? idsByNameAndPark))
        {
            idsByNameAndPark = new List<string>();
            context.CoasterIdsByNormalizedNameAndParkId[compositeKey] = idsByNameAndPark;
        }

        if (!idsByNameAndPark.Any(item => string.Equals(item, parkItemDocument.Id, StringComparison.Ordinal)))
        {
            idsByNameAndPark.Add(parkItemDocument.Id);
        }
    }

    private ParkDocument? ResolveOrCreateLocalParkForCoasterWithContext(
        string sessionId,
        CaptainCoasterCoasterSnapshotDocument externalCoaster,
        CaptainCoasterApplyExecutionContext context,
        DateTime utcNow)
    {
        ParkDocument? localParkDocument = null;
        CaptainCoasterParkSnapshotDocument? externalParkDocument = null;

        if (!string.IsNullOrWhiteSpace(externalCoaster.ParkCaptainCoasterId)
            && context.ParkSnapshotsByCaptainCoasterId.TryGetValue(
                externalCoaster.ParkCaptainCoasterId.Trim(),
                out List<CaptainCoasterParkSnapshotDocument>? externalParkVariants))
        {
            externalParkDocument = externalParkVariants.FirstOrDefault(item => item.SyncSessionId == sessionId) ?? externalParkVariants.FirstOrDefault();
            if (externalParkDocument != null)
            {
                localParkDocument = FindMatchingPark(context, externalParkDocument);
            }
        }

        if (localParkDocument == null && !string.IsNullOrWhiteSpace(externalCoaster.ParkName))
        {
            localParkDocument = FindMatchingParkByCoasterContext(context, externalCoaster);
        }

        if (localParkDocument != null)
        {
            return localParkDocument;
        }

        if (string.IsNullOrWhiteSpace(externalCoaster.ParkName))
        {
            return null;
        }

        localParkDocument = new ParkDocument
        {
            Name = externalParkDocument?.Name ?? externalCoaster.ParkName,
            CountryCode = NormalizeCountryCodeForStorage(externalParkDocument?.CountryCode ?? externalCoaster.CountryCode),
            Latitude = externalParkDocument?.Latitude,
            Longitude = externalParkDocument?.Longitude,
            IsVisible = false,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        localParkDocument.RefreshLocation();

        context.LocalParks.Add(localParkDocument);
        context.LocalParksById[localParkDocument.Id] = localParkDocument;
        AddParkLookup(context, localParkDocument);
        context.PendingParkWrites.Add(
            new ReplaceOneModel<ParkDocument>(
                Builders<ParkDocument>.Filter.Eq(item => item.Id, localParkDocument.Id),
                localParkDocument)
            {
                IsUpsert = true,
            });

        context.AffectedParkIds.Add(localParkDocument.Id);

        return localParkDocument;
    }

    private AttractionManufacturerDocument? ResolveManufacturerWithContext(
        string? manufacturerName,
        CaptainCoasterApplyExecutionContext context,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(manufacturerName))
        {
            return null;
        }

        string normalizedManufacturerName = Normalize(manufacturerName);
        if (context.ManufacturersByNormalizedName.TryGetValue(normalizedManufacturerName, out AttractionManufacturerDocument? manufacturer))
        {
            return manufacturer;
        }

        manufacturer = new AttractionManufacturerDocument
        {
            Name = manufacturerName.Trim(),
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

        context.ManufacturersByNormalizedName[normalizedManufacturerName] = manufacturer;
        context.PendingManufacturerWrites.Add(
            new ReplaceOneModel<AttractionManufacturerDocument>(
                Builders<AttractionManufacturerDocument>.Filter.Eq(item => item.Id, manufacturer.Id),
                manufacturer)
            {
                IsUpsert = true,
            });

        return manufacturer;
    }

    private sealed class CaptainCoasterApplyExecutionContext
    {
        public CaptainCoasterApplyExecutionContext(
            List<ParkDocument> localParks,
            List<ParkItemDocument> localCoasters,
            List<AttractionManufacturerDocument> manufacturers,
            List<CaptainCoasterParkSnapshotDocument> parkSnapshots,
            List<CaptainCoasterCoasterSnapshotDocument> coasterSnapshots)
        {
            this.LocalParks = localParks;
            this.LocalCoasters = localCoasters;
            this.LocalParksById = localParks.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);
            this.LocalCoastersById = localCoasters.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);
            this.ManufacturersByNormalizedName = manufacturers
                .GroupBy(item => Normalize(item.Name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            this.ParkSnapshotsById = parkSnapshots.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);
            this.ParkSnapshotsByCaptainCoasterId = parkSnapshots
                .Where(item => !string.IsNullOrWhiteSpace(item.CaptainCoasterId))
                .GroupBy(item => item.CaptainCoasterId.Trim(), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
            this.CoasterSnapshotsById = coasterSnapshots.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);
            this.CoasterSnapshotsByCaptainCoasterId = coasterSnapshots
                .Where(item => !string.IsNullOrWhiteSpace(item.CaptainCoasterId))
                .GroupBy(item => item.CaptainCoasterId.Trim(), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
            this.ParkIdsByNormalizedName = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.ParkIdsByNormalizedNameAndCountry = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.CoasterIdsByNormalizedName = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            this.CoasterIdsByNormalizedNameAndParkId = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (ParkDocument parkDocument in localParks)
            {
                AddParkLookup(this, parkDocument);
            }

            foreach (ParkItemDocument parkItemDocument in localCoasters)
            {
                AddCoasterLookup(this, parkItemDocument);
            }
        }

        public List<ParkDocument> LocalParks { get; }

        public List<ParkItemDocument> LocalCoasters { get; }

        public Dictionary<string, ParkDocument> LocalParksById { get; }

        public Dictionary<string, ParkItemDocument> LocalCoastersById { get; }

        public Dictionary<string, AttractionManufacturerDocument> ManufacturersByNormalizedName { get; }

        public Dictionary<string, CaptainCoasterParkSnapshotDocument> ParkSnapshotsById { get; }

        public Dictionary<string, List<CaptainCoasterParkSnapshotDocument>> ParkSnapshotsByCaptainCoasterId { get; }

        public Dictionary<string, CaptainCoasterCoasterSnapshotDocument> CoasterSnapshotsById { get; }

        public Dictionary<string, List<CaptainCoasterCoasterSnapshotDocument>> CoasterSnapshotsByCaptainCoasterId { get; }

        public Dictionary<string, List<string>> ParkIdsByNormalizedName { get; }

        public Dictionary<string, List<string>> ParkIdsByNormalizedNameAndCountry { get; }

        public Dictionary<string, List<string>> CoasterIdsByNormalizedName { get; }

        public Dictionary<string, List<string>> CoasterIdsByNormalizedNameAndParkId { get; }

        public List<WriteModel<ParkDocument>> PendingParkWrites { get; } = new List<WriteModel<ParkDocument>>();

        public List<WriteModel<ParkItemDocument>> PendingParkItemWrites { get; } = new List<WriteModel<ParkItemDocument>>();

        public List<WriteModel<AttractionManufacturerDocument>> PendingManufacturerWrites { get; } = new List<WriteModel<AttractionManufacturerDocument>>();

        public List<WriteModel<CaptainCoasterComparisonResultDocument>> PendingComparisonWrites { get; } = new List<WriteModel<CaptainCoasterComparisonResultDocument>>();

        public HashSet<string> AffectedParkIds { get; } = new HashSet<string>(StringComparer.Ordinal);

        public HashSet<string> AffectedParkItemIds { get; } = new HashSet<string>(StringComparer.Ordinal);
    }
}
