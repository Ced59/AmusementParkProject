using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Services.DataSources;

internal sealed partial class CaptainCoasterDataSourceProvider : IDataSourceProvider, IDataSourceImportExecutor
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

            if (localParkDocument == null)
            {
                localParkDocument = new ParkDocument
                {
                    Name = externalParkDocument.Name,
                    CountryCode = externalParkDocument.CountryCode,
                    Latitude = externalParkDocument.Latitude,
                    Longitude = externalParkDocument.Longitude,
                    IsVisible = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await localParksCollection.InsertOneAsync(localParkDocument, cancellationToken: cancellationToken);
            }
            else
            {
                localParkDocument.Name = externalParkDocument.Name;
                localParkDocument.CountryCode = externalParkDocument.CountryCode;
                localParkDocument.Latitude = externalParkDocument.Latitude;
                localParkDocument.Longitude = externalParkDocument.Longitude;
                localParkDocument.UpdatedAt = DateTime.UtcNow;
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
            List<ParkDocument> localParks = await localParksCollection.Find(Builders<ParkDocument>.Filter.Empty).ToListAsync(cancellationToken);
            List<ParkItemDocument> localCoasters = await localParkItemsCollection
                .Find(item => item.Category == ParkItemCategory.Attraction)
                .ToListAsync(cancellationToken);

            ParkItemDocument? localCoaster = null;
            if (!string.IsNullOrWhiteSpace(result.LocalEntityId))
            {
                localCoaster = localCoasters.FirstOrDefault(item => item.Id == result.LocalEntityId);
            }
            localCoaster ??= MatchCoaster(localCoasters, localParks, externalCoaster);

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
                        localParkDocument = new ParkDocument
                        {
                            Name = externalParkDocument.Name,
                            CountryCode = externalParkDocument.CountryCode,
                            Latitude = externalParkDocument.Latitude,
                            Longitude = externalParkDocument.Longitude,
                            IsVisible = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await localParksCollection.InsertOneAsync(localParkDocument, cancellationToken: cancellationToken);
                    }
                }
            }

            if (localParkDocument == null && !string.IsNullOrWhiteSpace(externalCoaster.ParkName))
            {
                localParkDocument = localParks.FirstOrDefault(item => Normalize(item.Name) == Normalize(externalCoaster.ParkName));
            }

            if (localParkDocument == null && !string.IsNullOrWhiteSpace(externalCoaster.ParkName))
            {
                localParkDocument = new ParkDocument
                {
                    Name = externalCoaster.ParkName,
                    CountryCode = null,
                    Latitude = 0d,
                    Longitude = 0d,
                    IsVisible = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
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
}
