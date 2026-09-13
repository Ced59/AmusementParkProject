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

internal static class CaptainCoasterApplyPipeline
{
    internal static async Task<CaptainCoasterApplyExecutionContext> BuildApplyExecutionContextAsync(this CaptainCoasterDataSourceProvider provider,
            string sessionId,
            CancellationToken cancellationToken)
    {
        List<ParkDocument> localParks = await provider.localParksCollection
            .Find(Builders<ParkDocument>.Filter.Empty)
            .ToListAsync(cancellationToken);

        List<ParkItemDocument> localCoasters = await provider.localParkItemsCollection
            .Find(item => item.Category == ParkItemCategory.Attraction)
            .ToListAsync(cancellationToken);

        List<AttractionManufacturerDocument> manufacturers = await provider.manufacturersCollection
            .Find(Builders<AttractionManufacturerDocument>.Filter.Empty)
            .ToListAsync(cancellationToken);

        List<CaptainCoasterParkSnapshotDocument> externalParks = await provider.parksCollection
            .Find(item => item.SyncSessionId == sessionId)
            .ToListAsync(cancellationToken);

        List<CaptainCoasterCoasterSnapshotDocument> externalCoasters = await provider.coastersCollection
            .Find(item => item.SyncSessionId == sessionId)
            .ToListAsync(cancellationToken);

        return new CaptainCoasterApplyExecutionContext(provider, localParks, localCoasters, manufacturers, externalParks, externalCoasters);
    }

    internal static bool HasPendingApplyWrites(this CaptainCoasterDataSourceProvider provider, CaptainCoasterApplyExecutionContext context, int batchSize)
    {
        return context.PendingParkWrites.Count >= batchSize
            || context.PendingParkItemWrites.Count >= batchSize
            || context.PendingManufacturerWrites.Count >= batchSize
            || context.PendingComparisonWrites.Count >= batchSize;
    }

    internal static async Task FlushApplyWritesAsync(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterApplyExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context.PendingParkWrites.Count > 0)
        {
            IReadOnlyCollection<ReplaceOneModel<ParkDocument>> replacements = context.PendingParkWrites
                .OfType<ReplaceOneModel<ParkDocument>>()
                .ToArray();
            await provider.FlushParkReplacementsAsync(replacements, cancellationToken);
            context.PendingParkWrites.Clear();
        }

        if (context.PendingParkItemWrites.Count > 0)
        {
            IReadOnlyCollection<ReplaceOneModel<ParkItemDocument>> replacements =
                context.PendingParkItemWrites
                .OfType<ReplaceOneModel<ParkItemDocument>>()
                .ToArray();
            await provider.FlushParkItemReplacementsAsync(replacements, cancellationToken);
            context.PendingParkItemWrites.Clear();
        }

        if (context.PendingManufacturerWrites.Count > 0)
        {
            await provider.manufacturersCollection.BulkWriteAsync(
                context.PendingManufacturerWrites,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken);

            context.PendingManufacturerWrites.Clear();
        }

        if (context.PendingComparisonWrites.Count > 0)
        {
            await provider.comparisonCollection.BulkWriteAsync(
                context.PendingComparisonWrites,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken);

            context.PendingComparisonWrites.Clear();
        }
    }

    internal static async Task FlushParkReplacementsAsync(this CaptainCoasterDataSourceProvider provider,
        IReadOnlyCollection<ReplaceOneModel<ParkDocument>> replacements,
        CancellationToken cancellationToken)
    {
        Dictionary<string, ParkDocument> pending = replacements
            .Select(static write => write.Replacement)
            .GroupBy(static document => document.Id, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.Last(), StringComparer.Ordinal);
        while (pending.Count > 0)
        {
            string[] parkIds = pending.Keys.ToArray();
            List<ParkDocument> previousDocuments = await provider.localParksCollection
                .Find(Builders<ParkDocument>.Filter.In(document => document.Id, parkIds))
                .ToListAsync(cancellationToken);
            IReadOnlyDictionary<string, ParkDocument> previousById = previousDocuments
                .ToDictionary(static document => document.Id, StringComparer.Ordinal);
            IReadOnlyCollection<Park> previousParks = previousDocuments
                .Select(static document => document.ToDomain())
                .ToArray();
            IReadOnlyCollection<Park> currentParks = pending.Values
                .Select(static document => document.ToDomain())
                .ToArray();
            RatingRankingMutationPreparation rankingPreparation =
                await provider.rankingSourceChangeCoordinator.PrepareParkChangesAsync(
                    previousParks,
                    currentParks,
                    cancellationToken);
            using CancellationTokenSource mutationCancellation =
                ShareSourceMutationCancellation.CreateLinkedSource(
                    cancellationToken,
                    rankingPreparation.ShareSourceMutationLeases);
            IReadOnlyCollection<WriteModel<ParkDocument>> writes = pending.Values
                .Select(document => BuildFencedParkReplacement(
                    document,
                    previousById.GetValueOrDefault(document.Id)))
                .ToArray();
            BulkWriteResult<ParkDocument> result;
            try
            {
                result = await ExecuteInsertAwareBulkWriteAsync(
                    provider.localParksCollection,
                    writes,
                    mutationCancellation.Token);
            }
            catch
            {
                await CompleteAmbiguousRankingMutationAsync(
                    provider.rankingSourceChangeCoordinator,
                    rankingPreparation);
                throw;
            }
            bool sourceChanged = HasSourceChanges(result);
            await provider.rankingSourceChangeCoordinator.CompleteMutationAsync(
                rankingPreparation,
                sourceChanged,
                CancellationToken.None);

            List<ParkDocument> committedDocuments = await provider.localParksCollection
                .Find(Builders<ParkDocument>.Filter.In(document => document.Id, parkIds))
                .ToListAsync(cancellationToken);
            foreach (ParkDocument committed in committedDocuments.Where(document =>
                         pending.TryGetValue(document.Id, out ParkDocument? replacement)
                         && DocumentsAreEquivalent(document, replacement)))
            {
                pending.Remove(committed.Id);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    internal static async Task FlushParkItemReplacementsAsync(this CaptainCoasterDataSourceProvider provider,
        IReadOnlyCollection<ReplaceOneModel<ParkItemDocument>> replacements,
        CancellationToken cancellationToken)
    {
        Dictionary<string, ParkItemDocument> pending = replacements
            .Select(static write => write.Replacement)
            .GroupBy(static document => document.Id, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.Last(), StringComparer.Ordinal);
        while (pending.Count > 0)
        {
            string[] parkItemIds = pending.Keys.ToArray();
            List<ParkItemDocument> previousDocuments = await provider.localParkItemsCollection
                .Find(Builders<ParkItemDocument>.Filter.In(document => document.Id, parkItemIds))
                .ToListAsync(cancellationToken);
            IReadOnlyDictionary<string, ParkItemDocument> previousById = previousDocuments
                .ToDictionary(static document => document.Id, StringComparer.Ordinal);
            IReadOnlyCollection<ParkItem> previousItems = previousDocuments
                .Select(static document => document.ToDomain())
                .ToArray();
            IReadOnlyCollection<ParkItem> currentItems = pending.Values
                .Select(static document => document.ToDomain())
                .ToArray();
            RatingRankingMutationPreparation rankingPreparation =
                await provider.rankingSourceChangeCoordinator.PrepareParkItemChangesAsync(
                    previousItems,
                    currentItems,
                    cancellationToken);
            using CancellationTokenSource mutationCancellation =
                ShareSourceMutationCancellation.CreateLinkedSource(
                    cancellationToken,
                    rankingPreparation.ShareSourceMutationLeases);
            IReadOnlyCollection<WriteModel<ParkItemDocument>> writes = pending.Values
                .Select(document => BuildFencedParkItemReplacement(
                    document,
                    previousById.GetValueOrDefault(document.Id)))
                .ToArray();
            BulkWriteResult<ParkItemDocument> result;
            try
            {
                result = await ExecuteInsertAwareBulkWriteAsync(
                    provider.localParkItemsCollection,
                    writes,
                    mutationCancellation.Token);
            }
            catch
            {
                await CompleteAmbiguousRankingMutationAsync(
                    provider.rankingSourceChangeCoordinator,
                    rankingPreparation);
                throw;
            }
            bool sourceChanged = HasSourceChanges(result);
            await provider.rankingSourceChangeCoordinator.CompleteMutationAsync(
                rankingPreparation,
                sourceChanged,
                CancellationToken.None);

            List<ParkItemDocument> committedDocuments = await provider.localParkItemsCollection
                .Find(Builders<ParkItemDocument>.Filter.In(document => document.Id, parkItemIds))
                .ToListAsync(cancellationToken);
            foreach (ParkItemDocument committed in committedDocuments.Where(document =>
                         pending.TryGetValue(document.Id, out ParkItemDocument? replacement)
                         && DocumentsAreEquivalent(document, replacement)))
            {
                pending.Remove(committed.Id);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    internal static async Task CompleteAmbiguousRankingMutationAsync(
        IRatingRankingSourceChangeCoordinator rankingSourceChangeCoordinator,
        RatingRankingMutationPreparation rankingPreparation)
    {
        ArgumentNullException.ThrowIfNull(rankingSourceChangeCoordinator);
        ArgumentNullException.ThrowIfNull(rankingPreparation);
        await rankingSourceChangeCoordinator.CompleteMutationAsync(
            rankingPreparation,
            sourceChanged: true,
            CancellationToken.None);
    }

    internal static WriteModel<ParkDocument> BuildFencedParkReplacement(
        ParkDocument replacement,
        ParkDocument? previousDocument)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        if (previousDocument is null)
        {
            return new InsertOneModel<ParkDocument>(replacement);
        }

        return new ReplaceOneModel<ParkDocument>(
            ParkRepository.BuildObservedRankingStateFilter(previousDocument),
            replacement)
        {
            IsUpsert = false,
        };
    }

    internal static WriteModel<ParkItemDocument> BuildFencedParkItemReplacement(
        ParkItemDocument replacement,
        ParkItemDocument? previousDocument)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        if (previousDocument is null)
        {
            return new InsertOneModel<ParkItemDocument>(replacement);
        }

        return new ReplaceOneModel<ParkItemDocument>(
            ParkItemRepository.BuildObservedRankingStateFilter(previousDocument),
            replacement)
        {
            IsUpsert = false,
        };
    }

    internal static async Task<BulkWriteResult<TDocument>> ExecuteInsertAwareBulkWriteAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IReadOnlyCollection<WriteModel<TDocument>> writes,
        CancellationToken cancellationToken)
    {
        try
        {
            return await collection.BulkWriteAsync(
                writes,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken);
        }
        catch (MongoBulkWriteException<TDocument> exception)
            when (exception.WriteConcernError is null
                && exception.WriteErrors.Count > 0
                && exception.WriteErrors.All(
                    static error => error.Category == ServerErrorCategory.DuplicateKey))
        {
            return exception.Result;
        }
    }

    internal static bool HasSourceChanges<TDocument>(BulkWriteResult<TDocument> result)
    {
        return result.InsertedCount > 0
            || result.ModifiedCount > 0
            || result.Upserts.Count > 0;
    }

    internal static bool DocumentsAreEquivalent<TDocument>(
        TDocument current,
        TDocument replacement)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(replacement);
        return current.ToBsonDocument().Equals(replacement.ToBsonDocument());
    }

    internal static CaptainCoasterApplyImpact ApplyParkResultWithContext(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution? resolution,
        CaptainCoasterApplyExecutionContext context,
        DateTime utcNow)
    {
        CaptainCoasterParkSnapshotDocument? externalParkDocument = provider.ResolveParkSnapshotWithContext(result, resolution, context);
        if (externalParkDocument == null)
        {
            return new CaptainCoasterApplyImpact { Applied = false };
        }

        ParkDocument? localParkDocument = null;
        if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
        {
            context.LocalParksById.TryGetValue(result.LocalEntityId.Trim(), out localParkDocument);
        }

        localParkDocument ??= provider.FindMatchingPark(context, externalParkDocument);

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
                UpdatedAt = utcNow,
            };
            localParkDocument.RefreshLocation();
            context.LocalParks.Add(localParkDocument);
            context.LocalParksById[localParkDocument.Id] = localParkDocument;
            provider.AddParkLookup(context, localParkDocument);
        }
        else
        {
            provider.ApplyExternalParkSnapshotToLocalPark(localParkDocument, externalParkDocument, utcNow);
            provider.AddParkLookup(context, localParkDocument);
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

    internal static CaptainCoasterApplyImpact ApplyCoasterResultWithContext(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterComparisonResultDocument result,
        DataSourceDuplicateResolution? resolution,
        CaptainCoasterApplyExecutionContext context,
        DateTime utcNow)
    {
        CaptainCoasterCoasterSnapshotDocument? externalCoaster = provider.ResolveCoasterSnapshotWithContext(result, resolution, context);
        if (externalCoaster == null)
        {
            return new CaptainCoasterApplyImpact { Applied = false };
        }

        ParkDocument? park = provider.ResolveOrCreateLocalParkForCoasterWithContext(result.SyncSessionId, externalCoaster, context, utcNow);
        if (park == null)
        {
            return new CaptainCoasterApplyImpact { Applied = false };
        }

        AttractionManufacturerDocument? manufacturer = provider.ResolveManufacturerWithContext(externalCoaster.Manufacturer, context, utcNow);

        ParkItemDocument? localCoaster = provider.ResolveSelectedLocalCoasterForImport(result, externalCoaster, park.Id, context);
        localCoaster ??= provider.FindMatchingLocalCoaster(context, externalCoaster, park.Id);

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
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            };
            context.LocalCoasters.Add(localCoaster);
            context.LocalCoastersById[localCoaster.Id] = localCoaster;
            provider.AddCoasterLookup(context, localCoaster);
        }
        else
        {
            localCoaster.Name = externalCoaster.Name;
            localCoaster.ParkId = park.Id;
            localCoaster.AttractionDetails = attractionDetails;
            localCoaster.UpdatedAt = utcNow;
            provider.AddCoasterLookup(context, localCoaster);
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

    internal static CaptainCoasterParkSnapshotDocument? ResolveParkSnapshotWithContext(this CaptainCoasterDataSourceProvider provider,
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

            return provider.BuildMergedParkSnapshot(result, resolution, variantsById, localParkDocument);
        }

        if (string.IsNullOrWhiteSpace(resolution.SelectedExternalVariantId))
        {
            return null;
        }

        variantsById.TryGetValue(resolution.SelectedExternalVariantId.Trim(), out CaptainCoasterParkSnapshotDocument? selected);
        return selected;
    }

    internal static CaptainCoasterCoasterSnapshotDocument? ResolveCoasterSnapshotWithContext(this CaptainCoasterDataSourceProvider provider,
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

            return provider.BuildMergedCoasterSnapshot(result, resolution, variantsById, localCoaster);
        }

        if (string.IsNullOrWhiteSpace(resolution.SelectedExternalVariantId))
        {
            return null;
        }

        variantsById.TryGetValue(resolution.SelectedExternalVariantId.Trim(), out CaptainCoasterCoasterSnapshotDocument? selected);
        return selected;
    }

    internal static ParkDocument? FindMatchingPark(this CaptainCoasterDataSourceProvider provider, CaptainCoasterApplyExecutionContext context, CaptainCoasterParkSnapshotDocument externalParkDocument)
    {
        string normalizedName = provider.Normalize(externalParkDocument.Name);
        string normalizedCountryCode = provider.Normalize(externalParkDocument.CountryCode);
        string compositeKey = provider.BuildParkCompositeKey(normalizedName, normalizedCountryCode);

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

    internal static ParkItemDocument? ResolveSelectedLocalCoasterForImport(this CaptainCoasterDataSourceProvider provider,
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

        return provider.IsSafeLocalCoasterImportMatch(selectedLocalCoaster, externalCoaster, targetParkId)
            ? selectedLocalCoaster
            : null;
    }

    internal static bool IsSafeLocalCoasterImportMatch(this CaptainCoasterDataSourceProvider provider,
        ParkItemDocument localCoaster,
        CaptainCoasterCoasterSnapshotDocument externalCoaster,
        string targetParkId)
    {
        if (!string.Equals(localCoaster.ParkId, targetParkId, StringComparison.Ordinal))
        {
            return false;
        }

        string normalizedExternalId = provider.Normalize(externalCoaster.CaptainCoasterId);
        if (!string.IsNullOrWhiteSpace(normalizedExternalId) && provider.IsCaptainCoasterLinkedTo(localCoaster, normalizedExternalId))
        {
            return true;
        }

        return provider.Normalize(localCoaster.Name) == provider.Normalize(externalCoaster.Name)
            && provider.IsPotentialCaptainCoasterTarget(localCoaster);
    }

    internal static ParkItemDocument? FindMatchingLocalCoaster(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterApplyExecutionContext context,
        CaptainCoasterCoasterSnapshotDocument externalCoaster,
        string targetParkId)
    {
        return provider.MatchCoasterInPark(context.LocalCoasters, targetParkId, externalCoaster);
    }

    internal static string BuildParkCompositeKey(this CaptainCoasterDataSourceProvider provider, string normalizedName, string normalizedCountryCode)
    {
        return $"{normalizedName}|{normalizedCountryCode}";
    }

    internal static string BuildCoasterCompositeKey(this CaptainCoasterDataSourceProvider provider, string normalizedName, string parkId)
    {
        return $"{normalizedName}|{parkId}";
    }

    internal static void AddParkLookup(this CaptainCoasterDataSourceProvider provider, CaptainCoasterApplyExecutionContext context, ParkDocument parkDocument)
    {
        string normalizedName = provider.Normalize(parkDocument.Name);
        string normalizedCountryCode = provider.Normalize(parkDocument.CountryCode);
        string compositeKey = provider.BuildParkCompositeKey(normalizedName, normalizedCountryCode);

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

    internal static ParkDocument? FindMatchingParkByCoasterContext(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterApplyExecutionContext context,
        CaptainCoasterCoasterSnapshotDocument externalCoaster)
    {
        if (string.IsNullOrWhiteSpace(externalCoaster.ParkName))
        {
            return null;
        }

        string normalizedParkName = provider.Normalize(externalCoaster.ParkName);
        string normalizedCountryCode = provider.Normalize(externalCoaster.CountryCode);

        if (!string.IsNullOrWhiteSpace(normalizedCountryCode))
        {
            string compositeKey = provider.BuildParkCompositeKey(normalizedParkName, normalizedCountryCode);
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

    internal static void AddCoasterLookup(this CaptainCoasterDataSourceProvider provider, CaptainCoasterApplyExecutionContext context, ParkItemDocument parkItemDocument)
    {
        string normalizedName = provider.Normalize(parkItemDocument.Name);
        string compositeKey = provider.BuildCoasterCompositeKey(normalizedName, parkItemDocument.ParkId);

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

    internal static ParkDocument? ResolveOrCreateLocalParkForCoasterWithContext(this CaptainCoasterDataSourceProvider provider,
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
                localParkDocument = provider.FindMatchingPark(context, externalParkDocument);
            }
        }

        if (localParkDocument == null && !string.IsNullOrWhiteSpace(externalCoaster.ParkName))
        {
            localParkDocument = provider.FindMatchingParkByCoasterContext(context, externalCoaster);
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
            CountryCode = provider.NormalizeCountryCodeForStorage(externalParkDocument?.CountryCode ?? externalCoaster.CountryCode),
            Latitude = externalParkDocument?.Latitude,
            Longitude = externalParkDocument?.Longitude,
            IsVisible = false,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        localParkDocument.RefreshLocation();

        context.LocalParks.Add(localParkDocument);
        context.LocalParksById[localParkDocument.Id] = localParkDocument;
        provider.AddParkLookup(context, localParkDocument);
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

    internal static AttractionManufacturerDocument? ResolveManufacturerWithContext(this CaptainCoasterDataSourceProvider provider,
        string? manufacturerName,
        CaptainCoasterApplyExecutionContext context,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(manufacturerName))
        {
            return null;
        }

        string normalizedManufacturerName = provider.Normalize(manufacturerName);
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



    // -----------------------------------------------------------------------
    // Comparison
    // -----------------------------------------------------------------------
}
