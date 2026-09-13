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

internal sealed class CaptainCoasterDataSourceProvider : IDataSourceProvider, IDataSourceImportExecutor
{
// -----------------------------------------------------------------------
        // Apply
        // -----------------------------------------------------------------------

        private async Task<CaptainCoasterApplyImpact> ApplyParkResultAsync(
            CaptainCoasterComparisonResultDocument result,
            DataSourceDuplicateResolution? resolution,
            CancellationToken cancellationToken)
        {
            CaptainCoasterParkSnapshotDocument? externalParkDocument = await ResolveParkSnapshotAsync(result, resolution, cancellationToken);
            if (externalParkDocument == null)
            {
                return new CaptainCoasterApplyImpact { Applied = false };
            }

            List<ParkDocument> localParks = await localParksCollection.Find(Builders<ParkDocument>.Filter.Empty).ToListAsync(cancellationToken);
            ParkDocument? localParkDocument = null;
            if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
            {
                localParkDocument = localParks.FirstOrDefault(item => item.Id == result.LocalEntityId);
            }
            localParkDocument ??= MatchPark(localParks, externalParkDocument);

            DateTime utcNow = DateTime.UtcNow;
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
                    UpdatedAt = utcNow
                };
                localParkDocument.RefreshLocation();
                await localParksCollection.InsertOneAsync(localParkDocument, cancellationToken: cancellationToken);
            }
            else
            {
                ApplyExternalParkSnapshotToLocalPark(localParkDocument, externalParkDocument, utcNow);
                await localParksCollection.ReplaceOneAsync(item => item.Id == localParkDocument.Id, localParkDocument, cancellationToken: cancellationToken);
            }

            result.IsApplied = true;
            result.LocalEntityId = localParkDocument.Id;
            result.AppliedExternalVariantId = externalParkDocument.Id;
            result.ResolutionStatus = result.RequiresManualResolution ? (resolution?.Strategy ?? "SelectVariant") : "Applied";
            result.UpdatedAt = DateTime.UtcNow;
            await comparisonCollection.ReplaceOneAsync(item => item.Id == result.Id, result, cancellationToken: cancellationToken);
            return new CaptainCoasterApplyImpact
            {
                Applied = true,
                ParkId = localParkDocument.Id,
            };
        }

        private async Task<CaptainCoasterApplyImpact> ApplyCoasterResultAsync(
            CaptainCoasterComparisonResultDocument result,
            DataSourceDuplicateResolution? resolution,
            CancellationToken cancellationToken)
        {
            CaptainCoasterCoasterSnapshotDocument? externalCoaster = await ResolveCoasterSnapshotAsync(result, resolution, cancellationToken);
            if (externalCoaster == null)
            {
                return new CaptainCoasterApplyImpact { Applied = false };
            }

            ParkDocument? park = await ResolveOrCreateLocalParkForCoasterAsync(result.SyncSessionId, externalCoaster, cancellationToken);
            if (park == null)
            {
                return new CaptainCoasterApplyImpact { Applied = false };
            }

            AttractionManufacturerDocument? manufacturer = await ResolveManufacturerAsync(externalCoaster.Manufacturer, cancellationToken);
            List<ParkItemDocument> localCoasters = await localParkItemsCollection
                .Find(item => item.Category == ParkItemCategory.Attraction)
                .ToListAsync(cancellationToken);

            ParkItemDocument? localCoaster = ResolveSelectedLocalCoasterForImport(result, externalCoaster, park.Id, localCoasters);
            localCoaster ??= MatchCoasterInPark(localCoasters, park.Id, externalCoaster);

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
            return new CaptainCoasterApplyImpact
            {
                Applied = true,
                ParkId = park.Id,
                ParkItemId = localCoaster.Id,
            };
        }

        private async Task<CaptainCoasterParkSnapshotDocument?> ResolveParkSnapshotAsync(
            CaptainCoasterComparisonResultDocument result,
            DataSourceDuplicateResolution? resolution,
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

            List<CaptainCoasterParkSnapshotDocument> parkVariants = await parksCollection
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
                    localParkDocument = await localParksCollection.Find(item => item.Id == result.LocalEntityId).FirstOrDefaultAsync(cancellationToken);
                }
                return BuildMergedParkSnapshot(result, resolution, variantsById, localParkDocument);
            }

            if (string.IsNullOrWhiteSpace(resolution.SelectedExternalVariantId))
            {
                return null;
            }

            variantsById.TryGetValue(resolution.SelectedExternalVariantId, out CaptainCoasterParkSnapshotDocument? selected);
            return selected;
        }

        private async Task<CaptainCoasterCoasterSnapshotDocument?> ResolveCoasterSnapshotAsync(
            CaptainCoasterComparisonResultDocument result,
            DataSourceDuplicateResolution? resolution,
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

            List<CaptainCoasterCoasterSnapshotDocument> coasterVariants = await coastersCollection
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
                    localCoaster = await localParkItemsCollection.Find(item => item.Id == result.LocalEntityId).FirstOrDefaultAsync(cancellationToken);
                }
                return BuildMergedCoasterSnapshot(result, resolution, variantsById, localCoaster);
            }

            if (string.IsNullOrWhiteSpace(resolution.SelectedExternalVariantId))
            {
                return null;
            }

            variantsById.TryGetValue(resolution.SelectedExternalVariantId, out CaptainCoasterCoasterSnapshotDocument? selected);
            return selected;
        }

        private static CaptainCoasterParkSnapshotDocument? BuildMergedParkSnapshot(
            CaptainCoasterComparisonResultDocument result,
            DataSourceDuplicateResolution resolution,
            IReadOnlyDictionary<string, CaptainCoasterParkSnapshotDocument> variantsById,
            ParkDocument? localParkDocument)
        {
            CaptainCoasterParkSnapshotDocument? baseVariant = GetBaseParkVariant(result, resolution, variantsById);
            if (baseVariant == null)
            {
                return null;
            }

            CaptainCoasterParkSnapshotDocument merged = CloneParkSnapshot(baseVariant);
            foreach (DataSourceFieldResolution fieldResolution in resolution.FieldResolutions)
            {
                ApplyParkFieldResolution(merged, fieldResolution, variantsById, localParkDocument);
            }

            merged.Id = Guid.NewGuid().ToString();
            merged.CreatedAt = DateTime.UtcNow;
            merged.UpdatedAt = DateTime.UtcNow;
            return merged;
        }

        private static CaptainCoasterCoasterSnapshotDocument? BuildMergedCoasterSnapshot(
            CaptainCoasterComparisonResultDocument result,
            DataSourceDuplicateResolution resolution,
            IReadOnlyDictionary<string, CaptainCoasterCoasterSnapshotDocument> variantsById,
            ParkItemDocument? localCoaster)
        {
            CaptainCoasterCoasterSnapshotDocument? baseVariant = GetBaseCoasterVariant(result, resolution, variantsById);
            if (baseVariant == null)
            {
                return null;
            }

            CaptainCoasterCoasterSnapshotDocument merged = CloneCoasterSnapshot(baseVariant);
            foreach (DataSourceFieldResolution fieldResolution in resolution.FieldResolutions)
            {
                ApplyCoasterFieldResolution(merged, fieldResolution, variantsById, localCoaster);
            }

            merged.Id = Guid.NewGuid().ToString();
            merged.CreatedAt = DateTime.UtcNow;
            merged.UpdatedAt = DateTime.UtcNow;
            return merged;
        }

        private static CaptainCoasterParkSnapshotDocument? GetBaseParkVariant(
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

        private static CaptainCoasterCoasterSnapshotDocument? GetBaseCoasterVariant(
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

        private static CaptainCoasterParkSnapshotDocument CloneParkSnapshot(CaptainCoasterParkSnapshotDocument source)
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

        private static CaptainCoasterCoasterSnapshotDocument CloneCoasterSnapshot(CaptainCoasterCoasterSnapshotDocument source)
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

        private static void ApplyParkFieldResolution(
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

        private static void ApplyCoasterFieldResolution(
            CaptainCoasterCoasterSnapshotDocument target,
            DataSourceFieldResolution fieldResolution,
            IReadOnlyDictionary<string, CaptainCoasterCoasterSnapshotDocument> variantsById,
            ParkItemDocument? localCoaster)
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
            if (!variantsById.TryGetValue(fieldResolution.ExternalVariantId, out CaptainCoasterCoasterSnapshotDocument? source))
            {
                return;
            }

            ApplyExternalCoasterField(target, fieldResolution.Field, source);
        }

        private static void ApplyLocalCoasterField(CaptainCoasterCoasterSnapshotDocument target, string field, ParkItemDocument? localCoaster)
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

        private static void ApplyExternalCoasterField(CaptainCoasterCoasterSnapshotDocument target, string field, CaptainCoasterCoasterSnapshotDocument source)
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

        private static ParkItemDocument? ResolveSelectedLocalCoasterForImport(
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

            return IsSafeLocalCoasterImportMatch(selectedLocalCoaster, externalCoaster, targetParkId)
                ? selectedLocalCoaster
                : null;
        }

        private static ParkDocument? MatchParkByCoasterContext(
            IEnumerable<ParkDocument> localParks,
            CaptainCoasterCoasterSnapshotDocument externalCoaster)
        {
            string normalizedParkName = Normalize(externalCoaster.ParkName);
            string normalizedCountryCode = Normalize(externalCoaster.CountryCode);
            List<ParkDocument> sameNameParks = localParks
                .Where(item => Normalize(item.Name) == normalizedParkName)
                .ToList();

            if (!string.IsNullOrWhiteSpace(normalizedCountryCode))
            {
                ParkDocument? sameCountryPark = sameNameParks
                    .FirstOrDefault(item => Normalize(item.CountryCode) == normalizedCountryCode);
                if (sameCountryPark != null)
                {
                    return sameCountryPark;
                }
            }

            return sameNameParks.Count == 1 ? sameNameParks[0] : null;
        }

        private async Task<ParkDocument?> ResolveOrCreateLocalParkForCoasterAsync(
            string sessionId,
            CaptainCoasterCoasterSnapshotDocument externalCoaster,
            CancellationToken cancellationToken)
        {
            List<ParkDocument> localParks = await localParksCollection.Find(Builders<ParkDocument>.Filter.Empty).ToListAsync(cancellationToken);
            ParkDocument? localParkDocument = null;

            if (!string.IsNullOrWhiteSpace(externalCoaster.ParkCaptainCoasterId))
            {
                CaptainCoasterParkSnapshotDocument? externalParkDocument = await parksCollection
                    .Find(item => item.SyncSessionId == sessionId && item.CaptainCoasterId == externalCoaster.ParkCaptainCoasterId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (externalParkDocument != null)
                {
                    localParkDocument = MatchPark(localParks, externalParkDocument);
                    if (localParkDocument == null)
                    {
                        DateTime utcNow = DateTime.UtcNow;
                        localParkDocument = new ParkDocument
                        {
                            Name = externalParkDocument.Name,
                            CountryCode = NormalizeCountryCodeForStorage(externalParkDocument.CountryCode),
                            Latitude = externalParkDocument.Latitude,
                            Longitude = externalParkDocument.Longitude,
                            IsVisible = false,
                            CreatedAt = utcNow,
                            UpdatedAt = utcNow
                        };
                        localParkDocument.RefreshLocation();
                        await localParksCollection.InsertOneAsync(localParkDocument, cancellationToken: cancellationToken);
                    }
                }
            }

            if (localParkDocument == null && !string.IsNullOrWhiteSpace(externalCoaster.ParkName))
            {
                localParkDocument = MatchParkByCoasterContext(localParks, externalCoaster);
            }

            if (localParkDocument == null && !string.IsNullOrWhiteSpace(externalCoaster.ParkName))
            {
                DateTime utcNow = DateTime.UtcNow;
                localParkDocument = new ParkDocument
                {
                    Name = externalCoaster.ParkName,
                    CountryCode = NormalizeCountryCodeForStorage(externalCoaster.CountryCode),
                    Latitude = null,
                    Longitude = null,
                    IsVisible = false,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow
                };
                localParkDocument.RefreshLocation();
                await localParksCollection.InsertOneAsync(localParkDocument, cancellationToken: cancellationToken);
            }

            return localParkDocument;
        }

        private async Task<AttractionManufacturerDocument?> ResolveManufacturerAsync(string? manufacturerName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(manufacturerName))
            {
                return null;
            }

            List<AttractionManufacturerDocument> manufacturers = await manufacturersCollection.Find(Builders<AttractionManufacturerDocument>.Filter.Empty).ToListAsync(cancellationToken);
            AttractionManufacturerDocument? manufacturer = manufacturers.FirstOrDefault(item => Normalize(item.Name) == Normalize(manufacturerName));
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
            IReadOnlyCollection<ReplaceOneModel<ParkDocument>> replacements = context.PendingParkWrites
                .OfType<ReplaceOneModel<ParkDocument>>()
                .ToArray();
            await this.FlushParkReplacementsAsync(replacements, cancellationToken);
            context.PendingParkWrites.Clear();
        }

        if (context.PendingParkItemWrites.Count > 0)
        {
            IReadOnlyCollection<ReplaceOneModel<ParkItemDocument>> replacements =
                context.PendingParkItemWrites
                .OfType<ReplaceOneModel<ParkItemDocument>>()
                .ToArray();
            await this.FlushParkItemReplacementsAsync(replacements, cancellationToken);
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

    private async Task FlushParkReplacementsAsync(
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
            List<ParkDocument> previousDocuments = await this.localParksCollection
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
                await this.rankingSourceChangeCoordinator.PrepareParkChangesAsync(
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
                    this.localParksCollection,
                    writes,
                    mutationCancellation.Token);
            }
            catch
            {
                await CompleteAmbiguousRankingMutationAsync(
                    this.rankingSourceChangeCoordinator,
                    rankingPreparation);
                throw;
            }
            bool sourceChanged = HasSourceChanges(result);
            await this.rankingSourceChangeCoordinator.CompleteMutationAsync(
                rankingPreparation,
                sourceChanged,
                CancellationToken.None);

            List<ParkDocument> committedDocuments = await this.localParksCollection
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

    private async Task FlushParkItemReplacementsAsync(
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
            List<ParkItemDocument> previousDocuments = await this.localParkItemsCollection
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
                await this.rankingSourceChangeCoordinator.PrepareParkItemChangesAsync(
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
                    this.localParkItemsCollection,
                    writes,
                    mutationCancellation.Token);
            }
            catch
            {
                await CompleteAmbiguousRankingMutationAsync(
                    this.rankingSourceChangeCoordinator,
                    rankingPreparation);
                throw;
            }
            bool sourceChanged = HasSourceChanges(result);
            await this.rankingSourceChangeCoordinator.CompleteMutationAsync(
                rankingPreparation,
                sourceChanged,
                CancellationToken.None);

            List<ParkItemDocument> committedDocuments = await this.localParkItemsCollection
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

    private static async Task<BulkWriteResult<TDocument>> ExecuteInsertAwareBulkWriteAsync<TDocument>(
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

    private static bool HasSourceChanges<TDocument>(BulkWriteResult<TDocument> result)
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

    internal static void AddParkLookup(CaptainCoasterApplyExecutionContext context, ParkDocument parkDocument)
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

    internal static void AddCoasterLookup(CaptainCoasterApplyExecutionContext context, ParkItemDocument parkItemDocument)
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



// -----------------------------------------------------------------------
        // Comparison
        // -----------------------------------------------------------------------

        private async Task<List<CaptainCoasterComparisonResultDocument>> BuildComparisonResultsAsync(
            string sessionId,
            IReadOnlyCollection<CaptainCoasterParkSnapshotDocument> externalParks,
            IReadOnlyCollection<CaptainCoasterCoasterSnapshotDocument> externalCoasters,
            CancellationToken cancellationToken)
        {
            List<CaptainCoasterComparisonResultDocument> results = new List<CaptainCoasterComparisonResultDocument>();
            List<ParkDocument> localParks = await localParksCollection.Find(Builders<ParkDocument>.Filter.Empty).ToListAsync(cancellationToken);
            List<ParkItemDocument> localCoasters = await localParkItemsCollection.Find(item => item.Category == ParkItemCategory.Attraction).ToListAsync(cancellationToken);
            List<AttractionManufacturerDocument> manufacturers = await manufacturersCollection.Find(Builders<AttractionManufacturerDocument>.Filter.Empty).ToListAsync(cancellationToken);
            Dictionary<string, AttractionManufacturerDocument> manufacturersById = manufacturers.ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);

            IEnumerable<IGrouping<string, CaptainCoasterParkSnapshotDocument>> parkGroups = externalParks
                .GroupBy(item => item.CaptainCoasterId, StringComparer.Ordinal);
            foreach (IGrouping<string, CaptainCoasterParkSnapshotDocument> group in parkGroups)
            {
                List<CaptainCoasterParkSnapshotDocument> variants = group.ToList();
                if (variants.Count == 1)
                {
                    CaptainCoasterParkSnapshotDocument externalParkDocument = variants[0];
                    ParkDocument? localParkDocument = MatchPark(localParks, externalParkDocument);
                    CaptainCoasterComparisonResultDocument compResult = BuildParkComparison(sessionId, localParkDocument, externalParkDocument);
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

            IEnumerable<IGrouping<string, CaptainCoasterCoasterSnapshotDocument>> coasterGroups = externalCoasters
                .GroupBy(item => item.CaptainCoasterId, StringComparer.Ordinal);
            foreach (IGrouping<string, CaptainCoasterCoasterSnapshotDocument> group in coasterGroups)
            {
                List<CaptainCoasterCoasterSnapshotDocument> variants = group.ToList();
                if (variants.Count == 1)
                {
                    CaptainCoasterCoasterSnapshotDocument externalCoaster = variants[0];
                    ParkItemDocument? localCoaster = MatchCoaster(localCoasters, localParks, externalParks, externalCoaster);
                    CaptainCoasterComparisonResultDocument compResult = BuildCoasterComparison(sessionId, localCoaster, externalCoaster, manufacturersById);
                    if (!string.Equals(compResult.ChangeType, "Identical", StringComparison.Ordinal))
                    {
                        results.Add(compResult);
                    }
                }
                else
                {
                    results.Add(BuildDuplicateCoasterComparison(sessionId, localCoasters, localParks, externalParks, manufacturersById, variants));
                }
            }

            return results;
        }

        private static CaptainCoasterComparisonResultDocument BuildParkComparison(string sessionId, ParkDocument? localParkDocument, CaptainCoasterParkSnapshotDocument externalParkDocument)
        {
            List<CaptainCoasterFieldChangeDocument> changes = BuildParkChanges(localParkDocument, externalParkDocument);
            string changeType = localParkDocument == null ? "MissingLocal" : (changes.Any(item => item.IsDifferent) ? "Updated" : "Identical");
            string matchConfidence = localParkDocument == null ? "None" : "High";

            return new CaptainCoasterComparisonResultDocument
            {
                SourceKey = SourceKeyValue,
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
                        DisplayLabel = BuildParkVariantLabel(externalParkDocument),
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

        private static CaptainCoasterComparisonResultDocument BuildDuplicateParkComparison(
            string sessionId,
            IReadOnlyCollection<ParkDocument> localParks,
            IReadOnlyCollection<CaptainCoasterParkSnapshotDocument> variants)
        {
            List<CaptainCoasterExternalVariantOptionDocument> options = variants
                .Select(variant => BuildParkVariantOption(localParks, variant))
                .ToList();
            MarkSuggestedVariant(options);

            CaptainCoasterExternalVariantOptionDocument? suggested = options.FirstOrDefault(item => item.IsSuggested) ?? options.FirstOrDefault();
            List<CaptainCoasterFieldChangeDocument> summaryChanges = new List<CaptainCoasterFieldChangeDocument>();
            AddChange(summaryChanges, "duplicateVariants", null, variants.Count.ToString(CultureInfo.InvariantCulture));

            return new CaptainCoasterComparisonResultDocument
            {
                SourceKey = SourceKeyValue,
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

        private static CaptainCoasterComparisonResultDocument BuildCoasterComparison(string sessionId, ParkItemDocument? localCoaster, CaptainCoasterCoasterSnapshotDocument externalCoaster, IReadOnlyDictionary<string, AttractionManufacturerDocument> manufacturersById)
        {
            List<CaptainCoasterFieldChangeDocument> changes = BuildCoasterChanges(localCoaster, externalCoaster, manufacturersById);
            string changeType = localCoaster == null ? "MissingLocal" : (changes.Any(item => item.IsDifferent) ? "Updated" : "Identical");
            string matchConfidence = localCoaster == null ? "None" : "Medium";

            return new CaptainCoasterComparisonResultDocument
            {
                SourceKey = SourceKeyValue,
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

        private static CaptainCoasterComparisonResultDocument BuildDuplicateCoasterComparison(
            string sessionId,
            IReadOnlyCollection<ParkItemDocument> localCoasters,
            IReadOnlyCollection<ParkDocument> localParks,
            IReadOnlyCollection<CaptainCoasterParkSnapshotDocument> externalParks,
            IReadOnlyDictionary<string, AttractionManufacturerDocument> manufacturersById,
            IReadOnlyCollection<CaptainCoasterCoasterSnapshotDocument> variants)
        {
            List<CaptainCoasterExternalVariantOptionDocument> options = variants
                .Select(variant => BuildCoasterVariantOption(localCoasters, localParks, externalParks, manufacturersById, variant))
                .ToList();
            MarkSuggestedVariant(options);

            CaptainCoasterExternalVariantOptionDocument? suggested = options.FirstOrDefault(item => item.IsSuggested) ?? options.FirstOrDefault();
            List<CaptainCoasterFieldChangeDocument> summaryChanges = new List<CaptainCoasterFieldChangeDocument>();
            AddChange(summaryChanges, "duplicateVariants", null, variants.Count.ToString(CultureInfo.InvariantCulture));

            return new CaptainCoasterComparisonResultDocument
            {
                SourceKey = SourceKeyValue,
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

        private static CaptainCoasterExternalVariantOptionDocument BuildParkVariantOption(
            IReadOnlyCollection<ParkDocument> localParks,
            CaptainCoasterParkSnapshotDocument variant)
        {
            ParkDocument? localParkDocument = MatchPark(localParks, variant);
            return new CaptainCoasterExternalVariantOptionDocument
            {
                ExternalVariantId = variant.Id,
                DisplayLabel = BuildParkVariantLabel(variant),
                CandidateLocalEntityId = localParkDocument?.Id,
                SourceUrl = variant.SourceUrl,
                Changes = BuildParkChanges(localParkDocument, variant)
            };
        }

        private static CaptainCoasterExternalVariantOptionDocument BuildCoasterVariantOption(
            IReadOnlyCollection<ParkItemDocument> localCoasters,
            IReadOnlyCollection<ParkDocument> localParks,
            IReadOnlyCollection<CaptainCoasterParkSnapshotDocument> externalParks,
            IReadOnlyDictionary<string, AttractionManufacturerDocument> manufacturersById,
            CaptainCoasterCoasterSnapshotDocument variant)
        {
            ParkItemDocument? localCoaster = MatchCoaster(localCoasters, localParks, externalParks, variant);
            return new CaptainCoasterExternalVariantOptionDocument
            {
                ExternalVariantId = variant.Id,
                DisplayLabel = BuildCoasterVariantLabel(variant),
                CandidateLocalEntityId = localCoaster?.Id,
                SourceUrl = variant.SourceUrl,
                Changes = BuildCoasterChanges(localCoaster, variant, manufacturersById)
            };
        }

        private static void MarkSuggestedVariant(List<CaptainCoasterExternalVariantOptionDocument> options)
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

        private static List<CaptainCoasterFieldChangeDocument> BuildParkChanges(ParkDocument? localParkDocument, CaptainCoasterParkSnapshotDocument externalParkDocument)
        {
            List<CaptainCoasterFieldChangeDocument> changes = new List<CaptainCoasterFieldChangeDocument>();
            AddChange(changes, "name", localParkDocument?.Name, externalParkDocument.Name);
            AddChange(changes, "countryCode", localParkDocument?.CountryCode, externalParkDocument.CountryCode);
            return changes;
        }

        private static List<CaptainCoasterFieldChangeDocument> BuildCoasterChanges(ParkItemDocument? localCoaster, CaptainCoasterCoasterSnapshotDocument externalCoaster, IReadOnlyDictionary<string, AttractionManufacturerDocument> manufacturersById)
        {
            List<CaptainCoasterFieldChangeDocument> changes = new List<CaptainCoasterFieldChangeDocument>();
            AddChange(changes, "name", localCoaster?.Name, externalCoaster.Name);
            string? localManufacturerName = ResolveManufacturerName(localCoaster?.AttractionDetails?.ManufacturerId, manufacturersById);
            AddChange(changes, "manufacturer", localManufacturerName, externalCoaster.Manufacturer);
            AddChange(changes, "model", localCoaster?.AttractionDetails?.Model, externalCoaster.Model);
            AddChange(changes, "externalSource", localCoaster?.AttractionDetails?.ExternalSource, LegacyExternalSourceValue);
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

        private static string BuildParkVariantLabel(CaptainCoasterParkSnapshotDocument externalParkDocument)
        {
            string country = string.IsNullOrWhiteSpace(externalParkDocument.CountryCode) ? externalParkDocument.CountryRaw ?? "?" : externalParkDocument.CountryCode;
            return $"{externalParkDocument.Name} — {country}";
        }

        private static string? ResolveManufacturerName(string? manufacturerId, IReadOnlyDictionary<string, AttractionManufacturerDocument> manufacturersById)
        {
            if (string.IsNullOrWhiteSpace(manufacturerId))
            {
                return null;
            }

            return manufacturersById.TryGetValue(manufacturerId, out AttractionManufacturerDocument? manufacturer)
                ? manufacturer.Name
                : manufacturerId;
        }

        private static string BuildCoasterVariantLabel(CaptainCoasterCoasterSnapshotDocument externalCoaster)
        {
            string parkName = string.IsNullOrWhiteSpace(externalCoaster.ParkName) ? "Parc inconnu" : externalCoaster.ParkName;
            string manufacturer = string.IsNullOrWhiteSpace(externalCoaster.Manufacturer) ? "Constructeur inconnu" : externalCoaster.Manufacturer;
            return $"{externalCoaster.Name} — {parkName} — {manufacturer}";
        }

        private static ParkDocument? MatchPark(IEnumerable<ParkDocument> localParks, CaptainCoasterParkSnapshotDocument externalParkDocument)
        {
            string normalizedName = Normalize(externalParkDocument.Name);
            string normalizedCountryCode = Normalize(externalParkDocument.CountryCode);

            List<ParkDocument> sameNameParks = localParks
                .Where(item => Normalize(item.Name) == normalizedName)
                .ToList();

            if (!string.IsNullOrWhiteSpace(normalizedCountryCode))
            {
                ParkDocument? sameCountryPark = sameNameParks
                    .FirstOrDefault(item => Normalize(item.CountryCode) == normalizedCountryCode);
                if (sameCountryPark != null)
                {
                    return sameCountryPark;
                }
            }

            return sameNameParks.Count == 1 ? sameNameParks[0] : null;
        }

        private static ParkItemDocument? MatchCoaster(
            IEnumerable<ParkItemDocument> localCoasters,
            IEnumerable<ParkDocument> localParks,
            IEnumerable<CaptainCoasterParkSnapshotDocument> externalParks,
            CaptainCoasterCoasterSnapshotDocument externalCoaster)
        {
            ParkDocument? localPark = ResolveLocalParkForCoaster(localParks, externalParks, externalCoaster);
            if (localPark == null)
            {
                return null;
            }

            return MatchCoasterInPark(localCoasters, localPark.Id, externalCoaster);
        }

        private static ParkDocument? ResolveLocalParkForCoaster(
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
                    ParkDocument? localPark = MatchPark(localParks, externalPark);
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

            string normalizedParkName = Normalize(externalCoaster.ParkName);
            string normalizedCountryCode = Normalize(externalCoaster.CountryCode);
            List<ParkDocument> sameNameParks = localParks
                .Where(item => Normalize(item.Name) == normalizedParkName)
                .ToList();

            if (!string.IsNullOrWhiteSpace(normalizedCountryCode))
            {
                ParkDocument? sameCountryPark = sameNameParks
                    .FirstOrDefault(item => Normalize(item.CountryCode) == normalizedCountryCode);
                if (sameCountryPark != null)
                {
                    return sameCountryPark;
                }
            }

            return sameNameParks.Count == 1 ? sameNameParks[0] : null;
        }

        private static ParkItemDocument? MatchCoasterInPark(
            IEnumerable<ParkItemDocument> localCoasters,
            string localParkId,
            CaptainCoasterCoasterSnapshotDocument externalCoaster)
        {
            string normalizedExternalId = Normalize(externalCoaster.CaptainCoasterId);
            string normalizedName = Normalize(externalCoaster.Name);
            List<ParkItemDocument> sameParkCoasters = localCoasters
                .Where(item => string.Equals(item.ParkId, localParkId, StringComparison.Ordinal))
                .ToList();

            if (!string.IsNullOrWhiteSpace(normalizedExternalId))
            {
                ParkItemDocument? sameExternalIdCoaster = sameParkCoasters
                    .FirstOrDefault(item => IsCaptainCoasterLinkedTo(item, normalizedExternalId));
                if (sameExternalIdCoaster != null)
                {
                    return sameExternalIdCoaster;
                }
            }

            return sameParkCoasters.FirstOrDefault(item =>
                Normalize(item.Name) == normalizedName
                && IsPotentialCaptainCoasterTarget(item));
        }

        private static bool IsCaptainCoasterLinkedTo(ParkItemDocument localCoaster, string normalizedExternalId)
        {
            if (localCoaster.AttractionDetails == null)
            {
                return false;
            }

            return string.Equals(Normalize(localCoaster.AttractionDetails.ExternalSource), Normalize(LegacyExternalSourceValue), StringComparison.Ordinal)
                && string.Equals(Normalize(localCoaster.AttractionDetails.ExternalId), normalizedExternalId, StringComparison.Ordinal);
        }

        private static bool IsPotentialCaptainCoasterTarget(ParkItemDocument localCoaster)
        {
            return localCoaster.Type == ParkItemType.RollerCoaster
                || string.Equals(Normalize(localCoaster.AttractionDetails?.ExternalSource), Normalize(LegacyExternalSourceValue), StringComparison.Ordinal);
        }

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
    private readonly IRatingRankingSourceChangeCoordinator rankingSourceChangeCoordinator;
    private readonly ILogger<CaptainCoasterDataSourceProvider> logger;

    public CaptainCoasterDataSourceProvider(
        IMongoDatabase database,
        MongoDbSettings mongoDbSettings,
        IDataSourceImportJobQueue queue,
        IDataAcquisitionHttpFetcher dataAcquisitionHttpFetcher,
        IXmlSitemapUrlDiscoveryService xmlSitemapUrlDiscoveryService,
        ICaptainCoasterCoasterPageParser coasterPageParser,
        ICaptainCoasterMapPageParser mapPageParser,
        ISearchProjectionWriter searchProjectionWriter,
        IRatingRankingSourceChangeCoordinator rankingSourceChangeCoordinator,
        ILogger<CaptainCoasterDataSourceProvider> logger)
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
        this.rankingSourceChangeCoordinator = rankingSourceChangeCoordinator;
        this.logger = logger;
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
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    failedCount++;
                    this.logger.LogWarning(exception, "Unable to apply Captain Coaster comparison result {ComparisonResultId} for session {SessionId}.", result.Id, session.Id);
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Captain Coaster apply failed for session {SessionId}.", session.Id);
            session.Status = "Failed";
            session.CurrentStep = "ApplyComparison";
            session.Message = $"Échec de l'application métier : {exception.Message}";
            session.ProgressPercentage = Math.Min(session.ProgressPercentage, 99);
            session.CompletedAtUtc = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.CanResume = true;
            session.Metrics.AppliedChanges = startingAppliedChanges + appliedCount;
            AddLog(session, "Error", $"Échec de l'application métier : {exception.Message}");
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            session.Status = "Canceled";
            session.CurrentStep = "Canceled";
            session.Message = "Import interrompu.";
            session.CompletedAtUtc = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.CanResume = true;
            AddLog(session, "Warn", session.Message);
            await this.PersistSessionAsync(session, CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Captain Coaster import failed for session {SessionId}.", session.Id);
            session.Status = "Failed";
            session.CurrentStep = "Failed";
            session.Message = exception.Message;
            session.CompletedAtUtc = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.CanResume = true;
            AddLog(session, "Error", $"Échec de l'import : {exception.Message}");
            await this.PersistSessionAsync(session, cancellationToken);
        }
        finally
        {
            DeleteWorkingDirectorySafe(job.ImportDescriptor.WorkingDirectoryPath);
        }
    }

private async Task ProcessCoasterPagesAsync(
        CaptainCoasterSyncSessionDocument session,
        IReadOnlyCollection<CaptainCoasterDiscoveredUrl> discoveredUrls,
        CaptainCoasterScrapingSettings scrapingSettings,
        CancellationToken cancellationToken)
    {
        List<CaptainCoasterCoasterSnapshotDocument> existingStagedCoasters = await this.coastersCollection
            .Find(item => item.SyncSessionId == session.Id)
            .ToListAsync(cancellationToken);

        HashSet<string> existingIds = existingStagedCoasters
            .Select(static item => item.CaptainCoasterId)
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<CaptainCoasterDiscoveredUrl> pendingUrls = discoveredUrls
            .Where(item => !existingIds.Contains(item.CaptainCoasterId))
            .ToList();

        int totalCount = discoveredUrls.Count;
        int processedCount = existingIds.Count;
        int skippedCount = totalCount - pendingUrls.Count;
        int failedCount = 0;

        session.Metrics.DiscoveredItems = totalCount;
        session.Metrics.SkippedItems = skippedCount;
        session.Metrics.ProcessedItems = processedCount;
        session.Metrics.FailedItems = failedCount;
        session.Metrics.CoastersFetched = processedCount;
        session.ProgressPercentage = CalculateFetchProgress(processedCount + skippedCount + failedCount, totalCount);
        session.Message = $"Pages coaster traitées : {processedCount}/{totalCount}.";
        session.UpdatedAt = DateTime.UtcNow;
        await this.PersistSessionAsync(session, cancellationToken);

        int maxConcurrentRequests = NormalizePositiveBounded(scrapingSettings.MaxConcurrentRequests, 4, 1, 16);
        int writeBatchSize = NormalizePositiveBounded(scrapingSettings.CoasterWriteBatchSize, 50, 5, 500);
        int progressSaveInterval = NormalizePositiveBounded(scrapingSettings.ProgressSaveInterval, 25, 1, 500);

        Channel<CaptainCoasterFetchOutcome> channel = Channel.CreateBounded<CaptainCoasterFetchOutcome>(new BoundedChannelOptions(writeBatchSize * Math.Max(2, maxConcurrentRequests))
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        });

        Task producerTask = Task.Run(async () =>
        {
            try
            {
                await Parallel.ForEachAsync(
                    pendingUrls,
                    new ParallelOptions
                    {
                        CancellationToken = cancellationToken,
                        MaxDegreeOfParallelism = maxConcurrentRequests,
                    },
                    async (discoveredUrl, ct) =>
                    {
                        try
                        {
                            string html = await this.dataAcquisitionHttpFetcher.GetStringAsync(
                                discoveredUrl.Url,
                                scrapingSettings.Language + ";q=1.0,en;q=0.8",
                                BuildRequestOptions(scrapingSettings),
                                ct);

                            CaptainCoasterParsedCoaster parsed = this.coasterPageParser.Parse(discoveredUrl, html, scrapingSettings);
                            CaptainCoasterCoasterSnapshotDocument document = MapParsedCoaster(session.Id, parsed);
                            await channel.Writer.WriteAsync(CaptainCoasterFetchOutcome.Success(document), ct);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            this.logger.LogWarning(exception, "Unable to fetch Captain Coaster URL {Url} for session {SessionId}.", discoveredUrl.Url, session.Id);
                            await channel.Writer.WriteAsync(CaptainCoasterFetchOutcome.Failure(discoveredUrl, exception.Message), ct);
                        }
                    });
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, cancellationToken);

        List<CaptainCoasterCoasterSnapshotDocument> writeBuffer = new List<CaptainCoasterCoasterSnapshotDocument>(writeBatchSize);
        int processedSinceLastSave = 0;

        await foreach (CaptainCoasterFetchOutcome outcome in channel.Reader.ReadAllAsync(cancellationToken))
        {
            if (outcome.Document is not null)
            {
                writeBuffer.Add(outcome.Document);
                processedCount++;
                processedSinceLastSave++;

                if (writeBuffer.Count >= writeBatchSize)
                {
                    await this.BulkUpsertCoastersAsync(session.Id, writeBuffer, cancellationToken);
                    writeBuffer.Clear();
                }
            }
            else
            {
                failedCount++;
                AddLog(session, "Error", $"Échec sur {outcome.DiscoveredUrl?.Url}: {outcome.ErrorMessage}");
            }

            session.Metrics.ProcessedItems = processedCount;
            session.Metrics.FailedItems = failedCount;
            session.Metrics.SkippedItems = skippedCount;
            session.Metrics.CoastersFetched = processedCount;
            session.ProgressPercentage = CalculateFetchProgress(processedCount + skippedCount + failedCount, totalCount);
            session.Message = $"Pages coaster traitées : {processedCount}/{totalCount}.";
            session.UpdatedAt = DateTime.UtcNow;

            if (processedSinceLastSave >= progressSaveInterval || failedCount > 0 && (processedCount + failedCount) % progressSaveInterval == 0)
            {
                AddLog(session, "Info", session.Message);
                await this.PersistSessionAsync(session, cancellationToken);
                processedSinceLastSave = 0;
            }
        }

        await producerTask;

        if (writeBuffer.Count > 0)
        {
            await this.BulkUpsertCoastersAsync(session.Id, writeBuffer, cancellationToken);
            writeBuffer.Clear();
        }

        List<CaptainCoasterCoasterSnapshotDocument> stagedCoasters = await this.coastersCollection
            .Find(item => item.SyncSessionId == session.Id)
            .ToListAsync(cancellationToken);

        List<CaptainCoasterParkSnapshotDocument> stagedParks = BuildParkSnapshots(session.Id, stagedCoasters);
        await this.parksCollection.DeleteManyAsync(item => item.SyncSessionId == session.Id, cancellationToken);
        if (stagedParks.Count > 0)
        {
            await this.parksCollection.InsertManyAsync(stagedParks, cancellationToken: cancellationToken);
        }

        session.Metrics.CoastersFetched = stagedCoasters.Count;
        session.Metrics.ParksFetched = stagedParks.Count;
        session.LastCompletedStep = "FetchCoasters";
        session.UpdatedAt = DateTime.UtcNow;
        AddLog(session, "Info", $"Staging reconstruit : {stagedParks.Count} parc(s), {stagedCoasters.Count} coaster(s).");
        await this.PersistSessionAsync(session, cancellationToken);
    }

    private async Task BulkUpsertCoastersAsync(
        string sessionId,
        IReadOnlyCollection<CaptainCoasterCoasterSnapshotDocument> documents,
        CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
        {
            return;
        }

        List<WriteModel<CaptainCoasterCoasterSnapshotDocument>> operations = documents
            .Select(document =>
                (WriteModel<CaptainCoasterCoasterSnapshotDocument>)new ReplaceOneModel<CaptainCoasterCoasterSnapshotDocument>(
                    Builders<CaptainCoasterCoasterSnapshotDocument>.Filter.Where(item => item.SyncSessionId == sessionId && item.CaptainCoasterId == document.CaptainCoasterId),
                    document)
                {
                    IsUpsert = true,
                })
            .ToList();

        await this.coastersCollection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
    }



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

        internal static string Normalize(string? value)
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

        private static string? NormalizeCountryCodeForStorage(string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return null;
            }

            return countryCode.Trim().ToUpperInvariant();
        }

        private static void ApplyExternalParkSnapshotToLocalPark(ParkDocument localParkDocument, CaptainCoasterParkSnapshotDocument externalParkDocument, DateTime utcNow)
        {
            localParkDocument.Name = externalParkDocument.Name;

            string? normalizedCountryCode = NormalizeCountryCodeForStorage(externalParkDocument.CountryCode);
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


    // ---------------------------------------------------------------------------
    // CountryNameMapper
    // ---------------------------------------------------------------------------


private static readonly IReadOnlyCollection<string> JsonImportSteps = new[] { "ParsingParks", "ParsingCoasters", "BuildComparison" };
    private static readonly IReadOnlyCollection<string> ScrapingImportSteps = new[] { "DiscoverUrls", "FetchCoasters", "EnrichParkCoordinates", "BuildComparison" };

    private static CaptainCoasterImportFiles ResolveInputFiles(DataSourceImportDescriptor importDescriptor)
    {
        if (!string.Equals(importDescriptor.ImportKind, "json-files", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Le mode d'import supporté ici est 'json-files'.", nameof(importDescriptor));
        }

        string parksFilePath = GetRequiredFile(importDescriptor.Files, new[] { "parks", "parksFile", "detected-parks.json", "detected-parks" });
        string coastersFilePath = GetRequiredFile(importDescriptor.Files, new[] { "coasters", "coastersFile", "coasters.json", "coasters" });
        return new CaptainCoasterImportFiles(parksFilePath, coastersFilePath);
    }

    private static string NormalizeImportKind(string? importKind)
    {
        if (string.IsNullOrWhiteSpace(importKind))
        {
            return "sitemap";
        }

        string normalized = importKind.Trim();
        if (string.Equals(normalized, "manual", StringComparison.OrdinalIgnoreCase))
        {
            return "manual-urls";
        }

        return normalized;
    }

    private static bool IsSupportedImportKind(string importKind)
    {
        return string.Equals(importKind, "json-files", StringComparison.OrdinalIgnoreCase)
            || string.Equals(importKind, "sitemap", StringComparison.OrdinalIgnoreCase)
            || string.Equals(importKind, "manual-urls", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<string> GetAvailableSteps(string importKind)
    {
        return string.Equals(importKind, "json-files", StringComparison.OrdinalIgnoreCase)
            ? JsonImportSteps
            : ScrapingImportSteps;
    }

    private static string GetRequiredFile(IReadOnlyCollection<DataSourceInputFileDescriptor> files, IReadOnlyCollection<string> acceptedKeys)
    {
        DataSourceInputFileDescriptor? match = files.FirstOrDefault(file =>
            acceptedKeys.Any(key => string.Equals(file.Key, key, StringComparison.OrdinalIgnoreCase))
            || acceptedKeys.Any(key => string.Equals(file.OriginalFileName, key, StringComparison.OrdinalIgnoreCase)));

        if (match is null || string.IsNullOrWhiteSpace(match.StoredFilePath) || !File.Exists(match.StoredFilePath))
        {
            throw new ArgumentException($"Fichier requis introuvable pour les clés : {string.Join(", ", acceptedKeys)}.", nameof(files));
        }

        return match.StoredFilePath;
    }

    private static void DeleteWorkingDirectorySafe(string? workingDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(workingDirectoryPath))
        {
            return;
        }

        try
        {
            if (Directory.Exists(workingDirectoryPath))
            {
                Directory.Delete(workingDirectoryPath, true);
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }

    private static DataSourceSettingsResult MapSettings(CaptainCoasterSettingsDocument settings)
    {
        Dictionary<string, string?> options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["baseUrl"] = settings.BaseUrl,
            ["apiKey"] = settings.ApiKey,
            ["dataDirectoryPath"] = settings.DataDirectoryPath,
            ["htmlDirectoryPath"] = settings.HtmlDirectoryPath,
            ["useOfflineMode"] = settings.UseOfflineMode.ToString(),
            ["sitemapUrl"] = settings.SitemapUrl ?? "https://captaincoaster.com/sitemap.xml",
            ["mapPageUrl"] = settings.MapPageUrl ?? "https://captaincoaster.com/fr/map/",
            ["delayBetweenRequestsMs"] = settings.DelayBetweenRequestsMs.ToString(CultureInfo.InvariantCulture),
            ["httpTimeoutSeconds"] = settings.HttpTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
            ["maxRetryCount"] = settings.MaxRetryCount.ToString(CultureInfo.InvariantCulture),
            ["maxConcurrentRequests"] = settings.MaxConcurrentRequests.ToString(CultureInfo.InvariantCulture),
            ["coasterWriteBatchSize"] = settings.CoasterWriteBatchSize.ToString(CultureInfo.InvariantCulture),
            ["progressSaveInterval"] = settings.ProgressSaveInterval.ToString(CultureInfo.InvariantCulture),
            ["maxCoasterCount"] = settings.MaxCoasterCount?.ToString(CultureInfo.InvariantCulture),
            ["skipCoasterCount"] = settings.SkipCoasterCount.ToString(CultureInfo.InvariantCulture),
            ["enrichParkCoordinates"] = settings.EnrichParkCoordinates.ToString(),
            ["mapMarkersAttributeName"] = settings.MapMarkersAttributeName,
            ["coasterTitleXPath"] = settings.CoasterTitleXPath,
            ["characteristicsItemXPath"] = settings.CharacteristicsItemXPath,
            ["characteristicLabelXPath"] = settings.CharacteristicLabelXPath,
            ["characteristicValueXPath"] = settings.CharacteristicValueXPath,
            ["topMetricXPath"] = settings.TopMetricXPath,
        };

        return new DataSourceSettingsResult
        {
            SourceKey = SourceKeyValue,
            DisplayName = DisplayNameValue,
            IsEnabled = settings.IsEnabled,
            Options = options,
        };
    }

    private static string? GetOption(IReadOnlyDictionary<string, string?> options, string key)
    {
        if (options.TryGetValue(key, out string? value))
        {
            return value;
        }

        return null;
    }

    private static bool TryParseBool(string? value)
    {
        return bool.TryParse(value, out bool parsed) && parsed;
    }

    private static int? TryParseInt(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        return null;
    }

    private static List<CaptainCoasterParkSnapshotDocument> ParseParksFromJson(string sessionId, byte[] jsonBytes)
    {
        List<CaptainCoasterParkSnapshotDocument> result = new List<CaptainCoasterParkSnapshotDocument>();
        JsonDocument document = JsonDocument.Parse(jsonBytes);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Le fichier detected-parks.json doit être un tableau JSON.");
        }

        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            string externalId = ReadString(element, "externalId") ?? string.Empty;
            string name = ReadString(element, "name") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            string? countryRaw = ReadString(element, "country");
            result.Add(new CaptainCoasterParkSnapshotDocument
            {
                SourceKey = SourceKeyValue,
                SyncSessionId = sessionId,
                CaptainCoasterId = externalId.Trim(),
                Name = name.Trim(),
                Slug = ReadString(element, "slug"),
                SourceUrl = ReadString(element, "sourceUrl"),
                CountryRaw = countryRaw,
                CountryCode = CountryNameMapper.ToCountryCode(countryRaw),
                Latitude = ReadDouble(element, "latitude"),
                Longitude = ReadDouble(element, "longitude"),
                CoasterCount = ReadInt(element, "coasterCount") ?? 0,
                SampleCoasterNames = ReadStringArray(element, "sampleCoasterNames"),
                ScrapedAtUtc = ReadDateTime(element, "scrapedAtUtc"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        return result;
    }

    private static List<CaptainCoasterCoasterSnapshotDocument> ParseCoastersFromJson(string sessionId, byte[] jsonBytes)
    {
        List<CaptainCoasterCoasterSnapshotDocument> result = new List<CaptainCoasterCoasterSnapshotDocument>();
        JsonDocument document = JsonDocument.Parse(jsonBytes);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Le fichier coasters.json doit être un tableau JSON.");
        }

        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            string externalId = ReadString(element, "externalId") ?? string.Empty;
            string name = ReadString(element, "name") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            string? countryRaw = ReadString(element, "country");
            result.Add(new CaptainCoasterCoasterSnapshotDocument
            {
                SourceKey = SourceKeyValue,
                SyncSessionId = sessionId,
                CaptainCoasterId = externalId.Trim(),
                Name = name.Trim(),
                Slug = ReadString(element, "slug"),
                SourceUrl = ReadString(element, "sourceUrl"),
                ParkCaptainCoasterId = ReadString(element, "parkSlug"),
                ParkName = ReadString(element, "parkName"),
                CountryRaw = NormalizeNullableText(countryRaw),
                CountryCode = CountryNameMapper.ToCountryCode(countryRaw),
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
                UpdatedAt = DateTime.UtcNow,
            });
        }

        return result;
    }

    private static string? NormalizeManufacturer(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        if (string.Equals(trimmed, "Inconnu", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return trimmed;
    }



private async Task ExecuteScrapingImportAsync(DataSourceImportJob job, CaptainCoasterSyncSessionDocument session, CancellationToken cancellationToken)
    {
        CaptainCoasterSettingsDocument settingsDocument = await this.GetOrCreateSettingsAsync();
        CaptainCoasterScrapingSettings scrapingSettings = BuildScrapingSettings(job.ImportDescriptor, settingsDocument);
        string startAtStep = NormalizeStartStep(GetOption(job.ImportDescriptor.Options, "startAtStep"));

        IReadOnlyCollection<CaptainCoasterDiscoveredUrl> discoveredUrls;
        if (ShouldRunStep(startAtStep, "DiscoverUrls"))
        {
            await this.UpdateSessionAsync(session, "DiscoverUrls", "Découverte des URLs à analyser.", 5, cancellationToken);
            discoveredUrls = await DiscoverUrlsAsync(job.ImportDescriptor, scrapingSettings, cancellationToken);
            await this.StageDiscoveredUrlsAsync(session.Id, discoveredUrls, cancellationToken);
            session.DiscoveredUrls = null;
            session.Metrics.DiscoveredItems = discoveredUrls.Count;
            session.LastCompletedStep = "DiscoverUrls";
            AddLog(session, "Info", $"{discoveredUrls.Count} URL(s) retenue(s) pour le traitement.");
            await this.PersistSessionAsync(session, cancellationToken);
        }
        else
        {
            discoveredUrls = await this.LoadDiscoveredUrlsAsync(session, scrapingSettings.Language, cancellationToken);

            if (discoveredUrls.Count == 0)
            {
                throw new InvalidOperationException("Aucune URL découverte n'est disponible pour reprendre le workflow depuis cette étape.");
            }
        }

        if (ShouldRunStep(startAtStep, "FetchCoasters"))
        {
            await this.UpdateSessionAsync(session, "FetchCoasters", "Téléchargement et parsing des pages coaster.", 15, cancellationToken);
            await ProcessCoasterPagesAsync(session, discoveredUrls, scrapingSettings, cancellationToken);
        }

        if (ShouldRunStep(startAtStep, "EnrichParkCoordinates"))
        {
            await this.UpdateSessionAsync(session, "EnrichParkCoordinates", "Enrichissement des coordonnées de parcs.", 75, cancellationToken);
            await EnrichParkCoordinatesAsync(session, scrapingSettings, cancellationToken);
        }

        if (ShouldRunStep(startAtStep, "BuildComparison"))
        {
            await this.UpdateSessionAsync(session, "BuildComparison", "Construction du rapport de comparaison.", 90, cancellationToken);
            await BuildComparisonFromStagingAsync(session, cancellationToken);
        }

        settingsDocument.LastSuccessfulSyncUtc = DateTime.UtcNow;
        settingsDocument.UpdatedAt = DateTime.UtcNow;
        await this.settingsCollection.ReplaceOneAsync(item => item.Id == settingsDocument.Id, settingsDocument, new ReplaceOptions { IsUpsert = true }, cancellationToken);

        session.Status = "Completed";
        session.CurrentStep = "Completed";
        session.Message = "Import Captain Coaster terminé. Les changements sont prêts pour validation manuelle.";
        session.ProgressPercentage = 100;
        session.CompletedAtUtc = DateTime.UtcNow;
        session.CanResume = true;
        session.UpdatedAt = DateTime.UtcNow;
        AddLog(session, "Info", $"Terminé : {session.Metrics.ParksFetched} parc(s), {session.Metrics.CoastersFetched} coaster(s), {session.Metrics.ComparisonResults} résultat(s). Les changements restent en attente de validation manuelle avant intégration métier.");
        await this.PersistSessionAsync(session, cancellationToken);
    }

    private async Task<IReadOnlyCollection<CaptainCoasterDiscoveredUrl>> DiscoverUrlsAsync(
        DataSourceImportDescriptor importDescriptor,
        CaptainCoasterScrapingSettings scrapingSettings,
        CancellationToken cancellationToken)
    {
        string importKind = NormalizeImportKind(importDescriptor.ImportKind);
        List<CaptainCoasterDiscoveredUrl> urls = new List<CaptainCoasterDiscoveredUrl>();

        if (string.Equals(importKind, "manual-urls", StringComparison.OrdinalIgnoreCase))
        {
            foreach (string url in importDescriptor.Urls)
            {
                CaptainCoasterDiscoveredUrl? parsed = CaptainCoasterScrapingUrlParser.TryParse(url, scrapingSettings.Language);
                if (parsed is not null)
                {
                    urls.Add(parsed);
                }
            }
        }
        else
        {
            string sitemapContent = await this.dataAcquisitionHttpFetcher.GetStringAsync(
                scrapingSettings.SitemapUrl,
                scrapingSettings.Language + ";q=1.0,en;q=0.8",
                BuildRequestOptions(scrapingSettings),
                cancellationToken);

            IReadOnlyCollection<string> sitemapUrls = this.xmlSitemapUrlDiscoveryService.ReadUrls(sitemapContent);
            foreach (string sitemapUrl in sitemapUrls)
            {
                CaptainCoasterDiscoveredUrl? parsed = CaptainCoasterScrapingUrlParser.TryParse(sitemapUrl, scrapingSettings.Language);
                if (parsed is not null)
                {
                    urls.Add(parsed);
                }
            }
        }

        List<CaptainCoasterDiscoveredUrl> orderedUrls = urls
            .GroupBy(static item => item.CaptainCoasterId, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(item => int.TryParse(item.CaptainCoasterId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ? id : int.MaxValue)
            .ThenBy(static item => item.CaptainCoasterId, StringComparer.OrdinalIgnoreCase)
            .Skip(scrapingSettings.SkipCoasterCount)
            .Take(scrapingSettings.MaxCoasterCount ?? int.MaxValue)
            .ToList();

        if (orderedUrls.Count == 0)
        {
            throw new InvalidOperationException("Aucune URL Captain Coaster valide n'a été trouvée pour cet import.");
        }

        return orderedUrls;
    }


    private async Task EnrichParkCoordinatesAsync(CaptainCoasterSyncSessionDocument session, CaptainCoasterScrapingSettings scrapingSettings, CancellationToken cancellationToken)
    {
        if (!scrapingSettings.EnrichParkCoordinates)
        {
            AddLog(session, "Info", "Enrichissement des coordonnées désactivé pour cette exécution.");
            session.LastCompletedStep = "EnrichParkCoordinates";
            await this.PersistSessionAsync(session, cancellationToken);
            return;
        }

        List<CaptainCoasterParkSnapshotDocument> parks = await this.parksCollection
            .Find(item => item.SyncSessionId == session.Id)
            .ToListAsync(cancellationToken);

        string html = await this.dataAcquisitionHttpFetcher.GetStringAsync(
            scrapingSettings.MapPageUrl,
            scrapingSettings.Language + ";q=1.0,en;q=0.8",
            BuildRequestOptions(scrapingSettings),
            cancellationToken);

        IReadOnlyCollection<CaptainCoasterParkCoordinate> coordinates = this.mapPageParser.Parse(scrapingSettings.MapPageUrl, html, scrapingSettings.MapMarkersAttributeName);
        Dictionary<string, CaptainCoasterParkCoordinate> coordinatesBySlug = coordinates
            .GroupBy(static item => item.Name.ToSlugValue(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (CaptainCoasterParkSnapshotDocument park in parks)
        {
            string slug = park.Name.ToSlugValue();
            if (coordinatesBySlug.TryGetValue(slug, out CaptainCoasterParkCoordinate? coordinate))
            {
                park.Latitude = coordinate.Latitude;
                park.Longitude = coordinate.Longitude;
                park.SourceUrl ??= coordinate.SourceUrl;
                park.RefreshLocation();
                park.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (parks.Count > 0)
        {
            List<WriteModel<CaptainCoasterParkSnapshotDocument>> operations = parks
                .Select(park =>
                {
                    park.RefreshLocation();
                    return (WriteModel<CaptainCoasterParkSnapshotDocument>)new ReplaceOneModel<CaptainCoasterParkSnapshotDocument>(
                        Builders<CaptainCoasterParkSnapshotDocument>.Filter.Eq(item => item.Id, park.Id),
                        park)
                    {
                        IsUpsert = false,
                    };
                })
                .ToList();

            await this.parksCollection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
        }

        session.LastCompletedStep = "EnrichParkCoordinates";
        AddLog(session, "Info", $"Coordonnées enrichies depuis la carte Captain Coaster pour {parks.Count(static item => item.Latitude.HasValue && item.Longitude.HasValue)} parc(s).");
        await this.PersistSessionAsync(session, cancellationToken);
    }

    private async Task BuildComparisonFromStagingAsync(CaptainCoasterSyncSessionDocument session, CancellationToken cancellationToken)
    {
        List<CaptainCoasterParkSnapshotDocument> parks = await this.parksCollection.Find(item => item.SyncSessionId == session.Id).ToListAsync(cancellationToken);
        List<CaptainCoasterCoasterSnapshotDocument> coasters = await this.coastersCollection.Find(item => item.SyncSessionId == session.Id).ToListAsync(cancellationToken);
        List<CaptainCoasterComparisonResultDocument> comparisonResults = await this.BuildComparisonResultsAsync(session.Id, parks, coasters, cancellationToken);
        await this.comparisonCollection.DeleteManyAsync(item => item.SyncSessionId == session.Id, cancellationToken);
        if (comparisonResults.Count > 0)
        {
            await this.comparisonCollection.InsertManyAsync(comparisonResults, cancellationToken: cancellationToken);
        }

        session.Metrics.ComparisonResults = comparisonResults.Count;
        session.Metrics.DuplicateConflicts = comparisonResults.Count(static item => item.RequiresManualResolution);
        session.LastCompletedStep = "BuildComparison";
        AddLog(session, "Info", $"{comparisonResults.Count} différence(s) détectée(s), dont {session.Metrics.DuplicateConflicts} conflit(s) nécessitant une résolution humaine.");
        await this.PersistSessionAsync(session, cancellationToken);
    }

    private static CaptainCoasterCoasterSnapshotDocument MapParsedCoaster(string sessionId, CaptainCoasterParsedCoaster parsed)
    {
        return new CaptainCoasterCoasterSnapshotDocument
        {
            SourceKey = SourceKeyValue,
            SyncSessionId = sessionId,
            CaptainCoasterId = parsed.ExternalId,
            Name = parsed.Name,
            Slug = parsed.Slug,
            SourceUrl = parsed.SourceUrl,
            ParkCaptainCoasterId = parsed.ParkSlug,
            ParkName = parsed.ParkName,
            CountryRaw = NormalizeNullableText(parsed.CountryRaw),
            CountryCode = CountryNameMapper.ToCountryCode(parsed.CountryRaw),
            Manufacturer = parsed.Manufacturer,
            Model = parsed.Model,
            MaterialType = parsed.MaterialType,
            SeatingType = parsed.SeatingType,
            LaunchType = parsed.LaunchType,
            Restraint = parsed.RestraintType,
            IsLaunched = parsed.IsLaunched ?? false,
            HeightInMeters = parsed.HeightInMeters,
            LengthInMeters = parsed.LengthInMeters,
            SpeedInKmH = parsed.SpeedInKmH,
            DropInMeters = null,
            InversionCount = parsed.InversionCount,
            Status = parsed.Status,
            OpeningDate = PartialDateParser.Parse(parsed.OpeningDateText),
            ClosingDate = PartialDateParser.Parse(parsed.ClosingDateText),
            ScrapedAtUtc = parsed.ScrapedAtUtc,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    private static List<CaptainCoasterParkSnapshotDocument> BuildParkSnapshots(string sessionId, IReadOnlyCollection<CaptainCoasterCoasterSnapshotDocument> coasters)
    {
        return coasters
            .Where(static item => !string.IsNullOrWhiteSpace(item.ParkName))
            .GroupBy(static item => item.ParkName!, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                string? countryRaw = PickMostFrequentNonBlank(group.Select(static item => item.CountryRaw));
                string? countryCode = PickMostFrequentNonBlank(group.Select(static item => item.CountryCode));
                if (string.IsNullOrWhiteSpace(countryCode))
                {
                    countryCode = CountryNameMapper.ToCountryCode(countryRaw);
                }

                CaptainCoasterParkSnapshotDocument document = new CaptainCoasterParkSnapshotDocument
                {
                    SourceKey = SourceKeyValue,
                    SyncSessionId = sessionId,
                    CaptainCoasterId = group.First().ParkCaptainCoasterId ?? group.Key.ToSlugValue(),
                    Name = group.Key,
                    Slug = group.First().ParkCaptainCoasterId,
                    SourceUrl = group.First().SourceUrl,
                    CountryRaw = countryRaw,
                    CountryCode = NormalizeCountryCodeForStorage(countryCode),
                    Latitude = null,
                    Longitude = null,
                    CoasterCount = group.Count(),
                    SampleCoasterNames = group.Select(static item => item.Name).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList(),
                    ScrapedAtUtc = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                document.RefreshLocation();
                return document;
            })
            .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? PickMostFrequentNonBlank(IEnumerable<string?> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim())
            .GroupBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(static group => group.Count())
            .ThenBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.Key)
            .FirstOrDefault();
    }

    private static string? NormalizeNullableText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static CaptainCoasterScrapingSettings BuildScrapingSettings(DataSourceImportDescriptor importDescriptor, CaptainCoasterSettingsDocument settings)
    {
        int? maxCoasterCountOverride = TryParseInt(GetOption(importDescriptor.Options, "maxCoasterCount"));
        int? skipCountOverride = TryParseInt(GetOption(importDescriptor.Options, "skipCoasterCount"));
        int? delayOverride = TryParseInt(GetOption(importDescriptor.Options, "delayBetweenRequestsMs"));
        int? timeoutOverride = TryParseInt(GetOption(importDescriptor.Options, "httpTimeoutSeconds"));
        int? retryOverride = TryParseInt(GetOption(importDescriptor.Options, "maxRetryCount"));
        int? concurrentOverride = TryParseInt(GetOption(importDescriptor.Options, "maxConcurrentRequests"));
        int? writeBatchOverride = TryParseInt(GetOption(importDescriptor.Options, "coasterWriteBatchSize"));
        int? progressSaveOverride = TryParseInt(GetOption(importDescriptor.Options, "progressSaveInterval"));

        return new CaptainCoasterScrapingSettings
        {
            SitemapUrl = GetOption(importDescriptor.Options, "sitemapUrl") ?? settings.SitemapUrl ?? "https://captaincoaster.com/sitemap.xml",
            MapPageUrl = GetOption(importDescriptor.Options, "mapPageUrl") ?? settings.MapPageUrl ?? "https://captaincoaster.com/fr/map/",
            Language = GetOption(importDescriptor.Options, "language") ?? "fr",
            DelayBetweenRequestsMs = Math.Max(0, delayOverride ?? settings.DelayBetweenRequestsMs),
            TimeoutSeconds = Math.Max(5, timeoutOverride ?? settings.HttpTimeoutSeconds),
            MaxRetryCount = Math.Max(1, retryOverride ?? settings.MaxRetryCount),
            MaxConcurrentRequests = Math.Clamp(concurrentOverride ?? settings.MaxConcurrentRequests, 1, 16),
            CoasterWriteBatchSize = Math.Clamp(writeBatchOverride ?? settings.CoasterWriteBatchSize, 5, 500),
            ProgressSaveInterval = Math.Clamp(progressSaveOverride ?? settings.ProgressSaveInterval, 1, 500),
            MaxCoasterCount = maxCoasterCountOverride ?? settings.MaxCoasterCount,
            SkipCoasterCount = Math.Max(0, skipCountOverride ?? settings.SkipCoasterCount),
            EnrichParkCoordinates = GetOption(importDescriptor.Options, "enrichParkCoordinates") is string value ? TryParseBool(value) : settings.EnrichParkCoordinates,
            MapMarkersAttributeName = GetOption(importDescriptor.Options, "mapMarkersAttributeName") ?? settings.MapMarkersAttributeName,
            CoasterTitleXPath = GetOption(importDescriptor.Options, "coasterTitleXPath") ?? settings.CoasterTitleXPath,
            CharacteristicsItemXPath = GetOption(importDescriptor.Options, "characteristicsItemXPath") ?? settings.CharacteristicsItemXPath,
            CharacteristicLabelXPath = GetOption(importDescriptor.Options, "characteristicLabelXPath") ?? settings.CharacteristicLabelXPath,
            CharacteristicValueXPath = GetOption(importDescriptor.Options, "characteristicValueXPath") ?? settings.CharacteristicValueXPath,
            TopMetricXPath = GetOption(importDescriptor.Options, "topMetricXPath") ?? settings.TopMetricXPath,
        };
    }

    private static bool ShouldRunStep(string startAtStep, string candidateStep)
    {
        return GetStepOrder(candidateStep) >= GetStepOrder(startAtStep);
    }

    private static string NormalizeStartStep(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "DiscoverUrls";
        }

        string trimmed = value.Trim();
        if (string.Equals(trimmed, "Coordinates", StringComparison.OrdinalIgnoreCase))
        {
            return "EnrichParkCoordinates";
        }

        if (string.Equals(trimmed, "RefreshSearchIndex", StringComparison.OrdinalIgnoreCase))
        {
            return "BuildComparison";
        }

        return trimmed;
    }

    private static int GetStepOrder(string step)
    {
        if (string.Equals(step, "DiscoverUrls", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }
        if (string.Equals(step, "FetchCoasters", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }
        if (string.Equals(step, "EnrichParkCoordinates", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }
        if (string.Equals(step, "BuildComparison", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }
        return 0;
    }

    private static int CalculateFetchProgress(int current, int total)
    {
        if (total <= 0)
        {
            return 70;
        }

        double ratio = Math.Clamp((double)current / total, 0d, 1d);
        return 15 + (int)Math.Round(ratio * 55d, MidpointRounding.AwayFromZero);
    }

private async Task RefreshSearchProjectionAsync(
        CaptainCoasterSyncSessionDocument? session,
        IReadOnlyCollection<string> parkIds,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        HashSet<string> normalizedParkIds = parkIds
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item.Trim())
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> normalizedParkItemIds = parkItemIds
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item.Trim())
            .ToHashSet(StringComparer.Ordinal);

        if (normalizedParkIds.Count == 0 && normalizedParkItemIds.Count == 0)
        {
            if (session != null)
            {
                AddLog(session, "Info", "Aucune entité locale n'a nécessité de rafraîchissement du search index.");
            }

            return;
        }

        try
        {
            if (normalizedParkIds.Count > 0)
            {
                List<string> normalizedParkIdList = normalizedParkIds.ToList();

                await this.searchProjectionWriter.UpsertManyAsync(
                    SearchProjectionResourceTypes.Parks,
                    normalizedParkIdList,
                    cancellationToken);

                List<string> relatedParkItemIds = await this.localParkItemsCollection
                    .Find(item => normalizedParkIdList.Contains(item.ParkId))
                    .Project(item => item.Id)
                    .ToListAsync(cancellationToken);

                foreach (string relatedParkItemId in relatedParkItemIds)
                {
                    if (!string.IsNullOrWhiteSpace(relatedParkItemId))
                    {
                        normalizedParkItemIds.Add(relatedParkItemId.Trim());
                    }
                }
            }

            if (normalizedParkItemIds.Count > 0)
            {
                await this.searchProjectionWriter.UpsertManyAsync(
                    SearchProjectionResourceTypes.ParkItems,
                    normalizedParkItemIds.ToList(),
                    cancellationToken);
            }

            if (session != null)
            {
                AddLog(
                    session,
                    "Info",
                    $"Index de recherche rafraîchi : {normalizedParkIds.Count} parc(s) et {normalizedParkItemIds.Count} park item(s)."
                );
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "Unable to refresh Captain Coaster search projection for session {SessionId}.", session?.Id);
            if (session != null)
            {
                AddLog(session, "Warn", $"Échec du rafraîchissement de l'index de recherche : {exception.Message}");
            }
        }
    }



private async Task StageDiscoveredUrlsAsync(
        string sessionId,
        IReadOnlyCollection<CaptainCoasterDiscoveredUrl> discoveredUrls,
        CancellationToken cancellationToken)
    {
        await this.discoveredUrlsCollection.DeleteManyAsync(item => item.SyncSessionId == sessionId, cancellationToken);

        if (discoveredUrls.Count == 0)
        {
            return;
        }

        List<CaptainCoasterDiscoveredUrlDocument> documents = discoveredUrls
            .Select((item, index) => new CaptainCoasterDiscoveredUrlDocument
            {
                SourceKey = SourceKeyValue,
                SyncSessionId = sessionId,
                CaptainCoasterId = item.CaptainCoasterId,
                Language = item.Language,
                Url = item.Url,
                Sequence = index,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DiscoveredAtUtc = DateTime.UtcNow,
            })
            .ToList();

        foreach (List<CaptainCoasterDiscoveredUrlDocument> batch in ChunkItems(documents, 500))
        {
            await this.discoveredUrlsCollection.InsertManyAsync(batch, cancellationToken: cancellationToken);
        }
    }

    private async Task<IReadOnlyCollection<CaptainCoasterDiscoveredUrl>> LoadDiscoveredUrlsAsync(
        CaptainCoasterSyncSessionDocument session,
        string language,
        CancellationToken cancellationToken)
    {
        List<CaptainCoasterDiscoveredUrlDocument> stagedUrls = await this.discoveredUrlsCollection
            .Find(item => item.SyncSessionId == session.Id)
            .SortBy(item => item.Sequence)
            .ToListAsync(cancellationToken);

        if (stagedUrls.Count > 0)
        {
            return stagedUrls
                .Select(item => new CaptainCoasterDiscoveredUrl
                {
                    Url = item.Url,
                    Language = item.Language,
                    CaptainCoasterId = item.CaptainCoasterId,
                    Slug = CaptainCoasterScrapingUrlParser.TryParse(item.Url, language)?.Slug ?? string.Empty,
                })
                .ToList();
        }

        List<string> legacyUrls = session.DiscoveredUrls ?? new List<string>();
        return legacyUrls
            .Select(url => CaptainCoasterScrapingUrlParser.TryParse(url, language))
            .Where(static item => item is not null)
            .Cast<CaptainCoasterDiscoveredUrl>()
            .ToList();
    }
}
