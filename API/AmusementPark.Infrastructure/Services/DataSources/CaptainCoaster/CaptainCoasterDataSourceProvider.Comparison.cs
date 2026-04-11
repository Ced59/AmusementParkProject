using System.Globalization;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;

internal sealed partial class CaptainCoasterDataSourceProvider : IDataSourceProvider, IDataSourceImportExecutor
{
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
                    ParkItemDocument? localCoaster = MatchCoaster(localCoasters, localParks, externalCoaster);
                    CaptainCoasterComparisonResultDocument compResult = BuildCoasterComparison(sessionId, localCoaster, externalCoaster, manufacturersById);
                    if (!string.Equals(compResult.ChangeType, "Identical", StringComparison.Ordinal))
                    {
                        results.Add(compResult);
                    }
                }
                else
                {
                    results.Add(BuildDuplicateCoasterComparison(sessionId, localCoasters, localParks, manufacturersById, variants));
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
            IReadOnlyDictionary<string, AttractionManufacturerDocument> manufacturersById,
            IReadOnlyCollection<CaptainCoasterCoasterSnapshotDocument> variants)
        {
            List<CaptainCoasterExternalVariantOptionDocument> options = variants
                .Select(variant => BuildCoasterVariantOption(localCoasters, localParks, manufacturersById, variant))
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
            IReadOnlyDictionary<string, AttractionManufacturerDocument> manufacturersById,
            CaptainCoasterCoasterSnapshotDocument variant)
        {
            ParkItemDocument? localCoaster = MatchCoaster(localCoasters, localParks, variant);
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
            return localParks.FirstOrDefault(item => Normalize(item.Name) == normalizedName && Normalize(item.CountryCode) == normalizedCountryCode)
                ?? localParks.FirstOrDefault(item => Normalize(item.Name) == normalizedName);
        }

        private static ParkItemDocument? MatchCoaster(IEnumerable<ParkItemDocument> localCoasters, IEnumerable<ParkDocument> localParks, CaptainCoasterCoasterSnapshotDocument externalCoaster)
        {
            string normalizedName = Normalize(externalCoaster.Name);
            string? localParkId = null;
            if (!string.IsNullOrWhiteSpace(externalCoaster.ParkName))
            {
                ParkDocument? park = localParks.FirstOrDefault(item => Normalize(item.Name) == Normalize(externalCoaster.ParkName));
                localParkId = park?.Id;
            }
            return localCoasters.FirstOrDefault(item => Normalize(item.Name) == normalizedName && (string.IsNullOrWhiteSpace(localParkId) || item.ParkId == localParkId))
                ?? localCoasters.FirstOrDefault(item => Normalize(item.Name) == normalizedName);
        }
}
