using System.Text.Json;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using System.Globalization;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Localization;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Core.Geo;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Parks.Services;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using System.Text;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;
public sealed class ParkGraphUpsertProcessor
{
    internal const string OpeningHoursEntityType = "ParkOpeningHours";
    internal const string OpeningHoursPropertyName = "openingHours";
    internal const string LegacyOpeningHoursPropertyName = "parkOpeningHours";
    internal const string OpeningHoursDateFormat = "yyyy-MM-dd";
    internal const string OpeningHoursTimeFormat = "HH:mm";
    internal const string PricingEntityType = "ParkPricing";
    internal const string PricingPropertyName = "pricing";
    internal const string LegacyPricingPropertyName = "parkPricing";
    internal const string PricingDateFormat = "yyyy-MM-dd";
    internal readonly IParkPricingRepository? parkPricingRepository;
    /// <summary>
    /// Constructor used by dependency injection when park pricing support is available.
    /// The legacy constructor remains available so existing focused tests can keep supplying
    /// only the dependencies required by the scenario they exercise.
    /// </summary>
    public ParkGraphUpsertProcessor(IParkRepository parkRepository, IParkZoneRepository parkZoneRepository, IParkItemRepository parkItemRepository, IParkFounderRepository parkFounderRepository, IParkOperatorRepository parkOperatorRepository, IAttractionManufacturerRepository attractionManufacturerRepository, IImageRepository imageRepository, IRemoteImageImporter remoteImageImporter, ISearchProjectionWriter searchProjectionWriter, IParkGraphUpsertHistoryRepository historyRepository, IPublicSeoUpdateNotifier publicSeoUpdateNotifier, IMeasurementConversionService measurementConversionService, IParkPricingRepository parkPricingRepository, IImageBinaryStorage imageBinaryStorage, IParkOpeningHoursRepository? parkOpeningHoursRepository = null, ParkOpeningHoursScheduleNormalizer? parkOpeningHoursScheduleNormalizer = null, ParkOpeningHoursCoverageSegmentBuilder? parkOpeningHoursCoverageSegmentBuilder = null, IHistoryEventRepository? historyEventRepository = null, IStandaloneAttractionRepository? standaloneAttractionRepository = null, ISocialPublicationService? socialPublicationService = null, IParkOfficialMapBinaryStorage? parkOfficialMapBinaryStorage = null, IParkOpeningHoursFactualChangeCapture? openingHoursFactualChangeCapture = null) : this(parkRepository, parkZoneRepository, parkItemRepository, parkFounderRepository, parkOperatorRepository, attractionManufacturerRepository, imageRepository, remoteImageImporter, searchProjectionWriter, historyRepository, publicSeoUpdateNotifier, measurementConversionService, parkOpeningHoursRepository, parkOpeningHoursScheduleNormalizer, parkOpeningHoursCoverageSegmentBuilder, historyEventRepository, standaloneAttractionRepository, socialPublicationService, imageBinaryStorage, parkOfficialMapBinaryStorage, openingHoursFactualChangeCapture)
    {
        this.parkPricingRepository = parkPricingRepository;
    }

    internal readonly IParkRepository parkRepository;
    internal readonly IParkZoneRepository parkZoneRepository;
    internal readonly IParkItemRepository parkItemRepository;
    internal readonly IParkFounderRepository parkFounderRepository;
    internal readonly IParkOperatorRepository parkOperatorRepository;
    internal readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    internal readonly IImageRepository imageRepository;
    internal readonly IRemoteImageImporter remoteImageImporter;
    internal readonly IImageBinaryStorage? imageBinaryStorage;
    internal readonly ISearchProjectionWriter searchProjectionWriter;
    internal readonly IParkGraphUpsertHistoryRepository historyRepository;
    internal readonly IPublicSeoUpdateNotifier publicSeoUpdateNotifier;
    internal readonly IStandaloneAttractionRepository? standaloneAttractionRepository;
    internal readonly IMeasurementConversionService measurementConversionService;
    internal readonly IParkOpeningHoursRepository? parkOpeningHoursRepository;
    internal readonly ParkOpeningHoursScheduleNormalizer? parkOpeningHoursScheduleNormalizer;
    internal readonly ParkOpeningHoursCoverageSegmentBuilder? parkOpeningHoursCoverageSegmentBuilder;
    internal readonly IParkOpeningHoursFactualChangeCapture? openingHoursFactualChangeCapture;
    internal readonly IHistoryEventRepository? historyEventRepository;
    internal readonly ISocialPublicationService? socialPublicationService;
    internal readonly IParkOfficialMapBinaryStorage? parkOfficialMapBinaryStorage;
    public ParkGraphUpsertProcessor(IParkRepository parkRepository, IParkZoneRepository parkZoneRepository, IParkItemRepository parkItemRepository, IParkFounderRepository parkFounderRepository, IParkOperatorRepository parkOperatorRepository, IAttractionManufacturerRepository attractionManufacturerRepository, IImageRepository imageRepository, IRemoteImageImporter remoteImageImporter, ISearchProjectionWriter searchProjectionWriter, IParkGraphUpsertHistoryRepository historyRepository, IPublicSeoUpdateNotifier publicSeoUpdateNotifier, IMeasurementConversionService measurementConversionService, IParkOpeningHoursRepository? parkOpeningHoursRepository = null, ParkOpeningHoursScheduleNormalizer? parkOpeningHoursScheduleNormalizer = null, ParkOpeningHoursCoverageSegmentBuilder? parkOpeningHoursCoverageSegmentBuilder = null, IHistoryEventRepository? historyEventRepository = null, IStandaloneAttractionRepository? standaloneAttractionRepository = null, ISocialPublicationService? socialPublicationService = null, IImageBinaryStorage? imageBinaryStorage = null, IParkOfficialMapBinaryStorage? parkOfficialMapBinaryStorage = null, IParkOpeningHoursFactualChangeCapture? openingHoursFactualChangeCapture = null)
    {
        this.parkRepository = parkRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.imageRepository = imageRepository;
        this.remoteImageImporter = remoteImageImporter;
        this.searchProjectionWriter = searchProjectionWriter;
        this.historyRepository = historyRepository;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
        this.measurementConversionService = measurementConversionService;
        this.parkOpeningHoursRepository = parkOpeningHoursRepository;
        this.parkOpeningHoursScheduleNormalizer = parkOpeningHoursScheduleNormalizer;
        this.parkOpeningHoursCoverageSegmentBuilder = parkOpeningHoursCoverageSegmentBuilder;
        this.openingHoursFactualChangeCapture = openingHoursFactualChangeCapture;
        this.historyEventRepository = historyEventRepository;
        this.standaloneAttractionRepository = standaloneAttractionRepository;
        this.socialPublicationService = socialPublicationService;
        this.imageBinaryStorage = imageBinaryStorage;
        this.parkOfficialMapBinaryStorage = parkOfficialMapBinaryStorage;
    }

    public async Task<ApplicationResult<ParkGraphUpsertResult>> PreviewAsync(ParkGraphUpsertRequest request, string? requestedByUserId, CancellationToken cancellationToken)
    {
        return await this.ProcessAsync(request, requestedByUserId, false, cancellationToken);
    }

    public async Task<ApplicationResult<ParkGraphUpsertResult>> ApplyAsync(ParkGraphUpsertRequest request, string? requestedByUserId, CancellationToken cancellationToken)
    {
        return await this.ProcessAsync(request, requestedByUserId, true, cancellationToken);
    }

    internal async Task<ApplicationResult<ParkGraphUpsertResult>> ProcessAsync(ParkGraphUpsertRequest request, string? requestedByUserId, bool apply, CancellationToken cancellationToken)
    {
        ParkGraphUpsertResult result = new ParkGraphUpsertResult
        {
            IsApplied = apply,
            AppliedAtUtc = apply ? DateTime.UtcNow : null,
        };
        if (request.Document.ValueKind != JsonValueKind.Object)
        {
            return ApplicationResult<ParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.InvalidDocument("Le document JSON racine doit être un objet."));
        }

        JsonElement root = request.Document;
        IReadOnlyCollection<string> encodedTextErrors = ParkGraphUpsertTextEncodingValidator.FindErrors(root);
        if (encodedTextErrors.Count > 0)
        {
            result.Errors.AddRange(encodedTextErrors);
            result.CanApply = false;
            ParkGraphUpsertProcessorResolutionExtensions.FinalizeCounts(result);
            await this.SaveHistoryAsync(request, requestedByUserId, apply, result, cancellationToken);
            return apply
                ? ApplicationResult<ParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.CannotApply("Le document ne peut pas être appliqué car il contient des caractères publics encodés en entités HTML."))
                : ApplicationResult<ParkGraphUpsertResult>.Success(result);
        }

        string mode = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(root, "mode") ?? "merge";
        result.Mode = mode;
        if (request.ReplaceCollections)
        {
            result.Warnings.Add("replaceCollections est reçu mais reste non destructif dans cette version : aucune zone ou aucun item absent du JSON n’est supprimé automatiquement.");
        }

        JsonElement? parkPatch = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, "park");
        JsonElement? identity = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, "identity");
        JsonElement? openingHoursPatch = ParkGraphUpsertProcessorOpeningHoursExtensions.ResolveOpeningHoursPatch(root);
        JsonElement? references = root.TryGetProperty("references", out JsonElement referencesElement) && referencesElement.ValueKind == JsonValueKind.Object ? referencesElement : null;
        bool requiresParkContext = ParkGraphUpsertProcessor.RequiresParkContext(root);
        Dictionary<string, string> founderKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> operatorKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> manufacturerKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> imageKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (ParkGraphUpsertProcessor.RequiresStandaloneAttractionContext(root))
        {
            await this.PreflightStandaloneHistoryAsync(root, request.CreateIfMissing, result, cancellationToken);
            if (result.Errors.Count > 0)
            {
                result.CanApply = false;
                ParkGraphUpsertProcessorResolutionExtensions.FinalizeCounts(result);
                await this.SaveHistoryAsync(request, requestedByUserId, apply, result, cancellationToken);
                return apply ? ApplicationResult<ParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.CannotApply("Le document ne peut pas etre applique car l'historique de l'attraction autonome est invalide.")) : ApplicationResult<ParkGraphUpsertResult>.Success(result);
            }

            await this.ProcessReferencesAsync(references, founderKeys, operatorKeys, manufacturerKeys, result, apply, cancellationToken);
            ParkGraphUpsertMergeSummary standaloneMergeSummary = await this.ProcessMergesAsync(root, manufacturerKeys, result, apply, cancellationToken);
            Dictionary<string, string> standaloneAttractionKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool standaloneAttractionChanged = await this.ProcessStandaloneAttractionAsync(root, request.CreateIfMissing, operatorKeys, manufacturerKeys, standaloneMergeSummary.ManufacturerIdRemaps, standaloneAttractionKeys, result, apply, cancellationToken);
            if (result.Errors.Count == 0)
            {
                await this.ProcessImagesAsync(root, null, standaloneAttractionKeys, founderKeys, operatorKeys, manufacturerKeys, standaloneMergeSummary.ManufacturerIdRemaps, imageKeys, result, apply, cancellationToken);
            }

            bool standaloneHistoryChanged = false;
            if (result.Errors.Count == 0)
            {
                standaloneHistoryChanged = await this.ProcessStandaloneHistoryEventsAsync(root, result.TargetStandaloneAttractionId, imageKeys, result, apply, cancellationToken);
            }

            ParkGraphUpsertProcessorResolutionExtensions.FinalizeCounts(result);
            if (apply && result.Errors.Count == 0)
            {
                await this.NotifyMergeSeoAsync(standaloneMergeSummary, cancellationToken);
                if (standaloneAttractionChanged || standaloneHistoryChanged)
                {
                    await this.publicSeoUpdateNotifier.NotifyAsync(new PublicSeoUpdate(), cancellationToken);
                }
            }

            await this.SaveHistoryAsync(request, requestedByUserId, apply, result, cancellationToken);
            return apply && result.Errors.Count > 0 ? ApplicationResult<ParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.CannotApply("Le document ne peut pas etre applique car l'attraction autonome est invalide.")) : ApplicationResult<ParkGraphUpsertResult>.Success(result);
        }

        string? targetParkId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(request.TargetParkId) ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(identity, "parkId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(identity, "id") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(parkPatch, "id") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(openingHoursPatch, "parkId");
        Park? targetPark = null;
        bool parkWillBeCreated = false;
        if (!string.IsNullOrWhiteSpace(targetParkId))
        {
            targetPark = await this.parkRepository.GetByIdAsync(targetParkId, true, cancellationToken);
            if (targetPark is null)
            {
                result.Errors.Add($"Aucun parc existant ne correspond à l'identifiant '{targetParkId}'.");
            }
        }
        else if (request.CreateIfMissing && requiresParkContext)
        {
            targetPark = ParkGraphUpsertProcessorPatchingExtensions.BuildNewParkFromPatch(parkPatch, identity, result);
            parkWillBeCreated = true;
        }
        else if (requiresParkContext)
        {
            result.Errors.Add("Aucun parc cible sélectionné. Sélectionner un parc existant ou activer la création explicite.");
        }

        if (targetPark is null)
        {
            if (!requiresParkContext)
            {
                await this.ProcessReferencesAsync(references, founderKeys, operatorKeys, manufacturerKeys, result, apply, cancellationToken);
                ParkGraphUpsertMergeSummary referenceFreeMergeSummary = await this.ProcessMergesAsync(root, manufacturerKeys, result, apply, cancellationToken);
                if (result.Errors.Count == 0)
                {
                    await this.ProcessImagesAsync(root, null, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), founderKeys, operatorKeys, manufacturerKeys, referenceFreeMergeSummary.ManufacturerIdRemaps, imageKeys, result, apply, cancellationToken);
                }

                ParkGraphUpsertProcessorResolutionExtensions.FinalizeCounts(result);
                if (apply && result.Errors.Count == 0)
                {
                    await this.NotifyMergeSeoAsync(referenceFreeMergeSummary, cancellationToken);
                }

                await this.SaveHistoryAsync(request, requestedByUserId, apply, result, cancellationToken);
                return apply && result.Errors.Count > 0 ? ApplicationResult<ParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.CannotApply("Le document ne peut pas etre applique car une fusion est invalide.")) : ApplicationResult<ParkGraphUpsertResult>.Success(result);
            }

            result.CanApply = false;
            ParkGraphUpsertProcessorResolutionExtensions.FinalizeCounts(result);
            await this.SaveHistoryAsync(request, requestedByUserId, apply, result, cancellationToken);
            return apply ? ApplicationResult<ParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.CannotApply("Le document ne peut pas être appliqué car aucun parc cible fiable n'a été résolu.")) : ApplicationResult<ParkGraphUpsertResult>.Success(result);
        }

        await this.PreflightOfficialMapsAsync(root, targetPark, parkPatch, result, cancellationToken);
        if (result.Errors.Count > 0)
        {
            result.TargetParkId = targetPark.Id;
            result.TargetParkName = targetPark.Name;
            result.CanApply = false;
            ParkGraphUpsertProcessorResolutionExtensions.FinalizeCounts(result);
            await this.SaveHistoryAsync(request, requestedByUserId, apply, result, cancellationToken);
            return apply ? ApplicationResult<ParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.CannotApply("Le document ne peut pas être appliqué car les cartes officielles sont invalides.")) : ApplicationResult<ParkGraphUpsertResult>.Success(result);
        }

        await this.ProcessReferencesAsync(references, founderKeys, operatorKeys, manufacturerKeys, result, apply, cancellationToken);
        ParkGraphUpsertMergeSummary mergeSummary = await this.ProcessMergesAsync(root, manufacturerKeys, result, apply, cancellationToken);
        targetPark = await this.RefreshTargetParkAfterAppliedMergesAsync(targetPark, mergeSummary, apply, cancellationToken);
        if (targetPark is null)
        {
            result.CanApply = false;
            ParkGraphUpsertProcessorResolutionExtensions.FinalizeCounts(result);
            await this.SaveHistoryAsync(request, requestedByUserId, apply, result, cancellationToken);
            return ApplicationResult<ParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.CannotApply("Le document ne peut pas être appliqué car le parc cible n'est plus disponible après les fusions."));
        }

        PublicSeoParkSnapshot? previousParkSnapshot = PublicSeoParkSnapshot.FromPark(targetPark);
        bool wasPubliclyDiscoverable = targetPark.IsPubliclyDiscoverable();
        ParkGraphUpsertChange parkChange = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("Park", targetPark.Id, "park", targetPark.Name ?? "Parc", parkWillBeCreated ? "Created" : "Unchanged", parkWillBeCreated ? "createIfMissing" : "id");
        ParkGraphUpsertProcessorPatchingExtensions.PatchPark(targetPark, parkPatch, identity, founderKeys, operatorKeys, parkChange, result, parkWillBeCreated);
        if (parkChange.Fields.Count > 0 || parkWillBeCreated)
        {
            parkChange.ChangeType = parkWillBeCreated ? "Created" : "Updated";
        }

        result.Changes.Add(parkChange);
        if (result.Errors.Count > 0)
        {
            result.CanApply = false;
            ParkGraphUpsertProcessorResolutionExtensions.FinalizeCounts(result);
            await this.SaveHistoryAsync(request, requestedByUserId, apply, result, cancellationToken);
            return apply ? ApplicationResult<ParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.CannotApply("Le document ne peut pas être appliqué car les données du parc sont invalides.")) : ApplicationResult<ParkGraphUpsertResult>.Success(result);
        }

        if (apply)
        {
            targetPark = parkWillBeCreated ? await this.parkRepository.CreateAsync(targetPark, cancellationToken) : await this.parkRepository.UpdateAsync(targetPark.Id, targetPark, cancellationToken) ?? targetPark;
            parkChange.EntityId = targetPark.Id;
        }

        result.TargetParkId = targetPark.Id;
        result.TargetParkName = targetPark.Name;
        await this.ProcessOpeningHoursAsync(root, targetPark, result, apply, cancellationToken);
        ParkGraphUpsertZoneSeoChanges zoneSeoChanges = await this.ProcessZonesAsync(root, targetPark, result, apply, cancellationToken);
        Dictionary<string, string> zoneKeys = zoneSeoChanges.ZoneKeys;
        Dictionary<string, string> itemKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ParkGraphUpsertItemSeoChanges itemSeoChanges = await this.ProcessItemsAsync(root, targetPark, zoneKeys, manufacturerKeys, mergeSummary.ManufacturerIdRemaps, itemKeys, result, apply, cancellationToken);
        await this.ProcessImagesAsync(root, targetPark, itemKeys, founderKeys, operatorKeys, manufacturerKeys, mergeSummary.ManufacturerIdRemaps, imageKeys, result, apply, cancellationToken);
        await this.ProcessHistoryEventsAsync(root, targetPark, itemKeys, imageKeys, result, apply, cancellationToken);
        ParkGraphUpsertItemSeoChanges deletionSeoChanges = await this.ProcessDeletionsAsync(root, targetPark, result, apply, cancellationToken);
        itemSeoChanges.MergeFrom(deletionSeoChanges);
        if (apply)
        {
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, targetPark.Id, cancellationToken);
            if (itemSeoChanges.ChangedItemIds.Count > 0)
            {
                await this.searchProjectionWriter.UpsertManyAsync(SearchProjectionResourceTypes.ParkItems, itemSeoChanges.ChangedItemIds, cancellationToken);
            }

            if (result.Changes.Any(static change => !string.Equals(change.ChangeType, "Unchanged", StringComparison.Ordinal)))
            {
                IReadOnlyCollection<PublicSeoParkSnapshot> previousParks = previousParkSnapshot is null ? mergeSummary.PreviousParks : new[]
                {
                    previousParkSnapshot
                }.Concat(mergeSummary.PreviousParks).ToList();
                await this.publicSeoUpdateNotifier.NotifyAsync(new PublicSeoUpdate { PreviousParks = previousParks, CurrentParks = PublicSeoParkSnapshot.FromParks(new[] { targetPark }).Concat(mergeSummary.CurrentParks).ToList(), PreviousParkItems = itemSeoChanges.PreviousItems.Concat(mergeSummary.PreviousParkItems).ToList(), CurrentParkItems = itemSeoChanges.CurrentItems.Concat(mergeSummary.CurrentParkItems).ToList(), PreviousParkZones = zoneSeoChanges.PreviousZones, CurrentParkZones = zoneSeoChanges.CurrentZones, IncludeDiscoveryPages = true, }, cancellationToken);
            }

            await this.PublishNewlyVisibleParkAsync(targetPark, wasPubliclyDiscoverable, requestedByUserId, result, cancellationToken);
        }

        ParkGraphUpsertProcessorResolutionExtensions.FinalizeCounts(result);
        await this.SaveHistoryAsync(request, requestedByUserId, apply, result, cancellationToken);
        return ApplicationResult<ParkGraphUpsertResult>.Success(result);
    }

    internal async Task PreflightOfficialMapsAsync(JsonElement root, Park park, JsonElement? parkPatch, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(parkPatch, "officialMaps"))
        {
            return;
        }

        (Park Park, IReadOnlyDictionary<string, string> StorageLookupKeys) projection = await this.ProjectOfficialMapTargetAfterMergesAsync(root, park, result, cancellationToken);
        if (result.Errors.Count > 0)
        {
            return;
        }

        Park candidate = ParkGraphUpsertProcessorMergeHelpersExtensions.ClonePark(projection.Park);
        ParkGraphUpsertResult preflightResult = new ParkGraphUpsertResult();
        ParkGraphOfficialMapUpsertPatcher.Patch(candidate, parkPatch, preflightResult);
        if (preflightResult.Errors.Count > 0)
        {
            result.Errors.AddRange(preflightResult.Errors);
            return;
        }

        IReadOnlyCollection<string> officialMapIds = preflightResult.Changes.Where(static change => string.Equals(change.EntityType, "ParkOfficialMap", StringComparison.Ordinal)).Select(static change => change.EntityId).Where(static id => !string.IsNullOrWhiteSpace(id)).Select(static id => id!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        await this.ValidateOfficialMapStorageAsync(candidate, officialMapIds, projection.StorageLookupKeys, result, cancellationToken);
    }

    internal async Task ValidateOfficialMapStorageAsync(Park park, IReadOnlyCollection<string> officialMapIds, IReadOnlyDictionary<string, string> storageLookupKeys, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        List<ParkOfficialMap> storedMaps = park.OfficialMaps.Where(officialMap => officialMapIds.Contains(officialMap.Id, StringComparer.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(officialMap.StorageKey)).ToList();
        if (storedMaps.Count == 0)
        {
            return;
        }

        if (this.parkOfficialMapBinaryStorage is null)
        {
            result.Errors.Add("La validation du stockage des cartes officielles n'est pas disponible dans ce contexte.");
            return;
        }

        foreach (ParkOfficialMap officialMap in storedMaps)
        {
            string storageLookupKey = storageLookupKeys.TryGetValue(officialMap.StorageKey!, out string? projectedSourceKey) ? projectedSourceKey : officialMap.StorageKey!;
            ParkOfficialMapBinaryMetadata? metadata = await this.parkOfficialMapBinaryStorage.GetMetadataAsync(storageLookupKey, cancellationToken);
            if (metadata is null)
            {
                result.Errors.Add($"Le fichier stocké de la carte officielle '{officialMap.Id}' est introuvable.");
                continue;
            }

            if (officialMap.SizeInBytes != metadata.SizeInBytes)
            {
                result.Errors.Add($"sizeInBytes ne correspond pas au fichier stocké de la carte officielle '{officialMap.Id}'.");
            }

            if (!string.Equals(officialMap.ContentType?.Trim(), metadata.ContentType.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add($"contentType ne correspond pas au fichier stocké de la carte officielle '{officialMap.Id}'.");
            }
        }
    }

    internal async Task ProcessReferencesAsync(JsonElement? references, Dictionary<string, string> founderKeys, Dictionary<string, string> operatorKeys, Dictionary<string, string> manufacturerKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (references is null)
        {
            return;
        }

        await this.ProcessFoundersAsync(references.Value, founderKeys, result, apply, cancellationToken);
        await this.ProcessOperatorsAsync(references.Value, operatorKeys, result, apply, cancellationToken);
        await this.ProcessManufacturersAsync(references.Value, manufacturerKeys, result, apply, cancellationToken);
    }

    internal static bool RequiresParkContext(JsonElement root)
    {
        if (ParkGraphUpsertProcessor.HasNonEmptyObject(root, "identity") || ParkGraphUpsertProcessor.HasNonEmptyObject(root, "park") || ParkGraphUpsertProcessorOpeningHoursExtensions.HasOpeningHoursPatch(root) || ParkGraphUpsertProcessor.HasHistoryPatch(root) || ParkGraphUpsertProcessor.HasNonEmptyArray(root, "zones") || ParkGraphUpsertProcessor.HasNonEmptyArray(root, "items") || ParkGraphUpsertProcessor.HasNonEmptyArray(root, "suppr") || ParkGraphUpsertProcessor.HasNonEmptyObject(root, "suppr") || ParkGraphUpsertProcessor.HasNonEmptyArray(root, "deletions") || ParkGraphUpsertProcessor.HasNonEmptyObject(root, "deletions"))
        {
            return true;
        }

        JsonElement? images = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(root, "images");
        if (images is null)
        {
            return false;
        }

        foreach (JsonElement image in images.Value.EnumerateArray())
        {
            if (ParkGraphUpsertProcessor.ImageRequiresParkContext(image))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool RequiresStandaloneAttractionContext(JsonElement root)
    {
        string? documentType = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(root, "documentType") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(root, "entityType");
        if (string.Equals(documentType, "standaloneAttraction", StringComparison.OrdinalIgnoreCase) || string.Equals(documentType, "standalone-attraction", StringComparison.OrdinalIgnoreCase) || string.Equals(documentType, "standaloneAttractionGraph", StringComparison.OrdinalIgnoreCase) || string.Equals(documentType, "standalone-attraction-graph", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ParkGraphUpsertProcessor.HasNonEmptyObject(root, "standaloneAttraction") || ParkGraphUpsertProcessor.HasNonEmptyArray(root, "standaloneAttractions") || ParkGraphUpsertProcessor.HasNonEmptyObject(root, "migration") || ParkGraphUpsertProcessor.HasNonEmptyObject(root, "standaloneAttractionMigration");
    }

    internal static bool ImageRequiresParkContext(JsonElement patch)
    {
        if (patch.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        string? ownerKey = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "ownerKey");
        if (string.Equals(ownerKey, "park", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        ImageOwnerType requestedOwnerType = ParkGraphUpsertProcessorImagesExtensions.ResolveRequestedImageOwnerType(patch);
        if (requestedOwnerType == ImageOwnerType.Park || requestedOwnerType == ImageOwnerType.ParkItem)
        {
            return true;
        }

        if (requestedOwnerType == ImageOwnerType.ParkOperator || requestedOwnerType == ImageOwnerType.ParkFounder || requestedOwnerType == ImageOwnerType.AttractionManufacturer)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(ownerKey);
    }

    internal static bool HasNonEmptyObject(JsonElement root, string propertyName)
    {
        JsonElement? value = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, propertyName);
        return value is not null && value.Value.EnumerateObject().Any();
    }

    internal static bool HasNonEmptyArray(JsonElement root, string propertyName)
    {
        JsonElement? value = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(root, propertyName);
        return value is not null && value.Value.GetArrayLength() > 0;
    }

    internal static bool HasHistoryPatch(JsonElement root)
    {
        JsonElement? history = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, "history");
        return ParkGraphUpsertProcessor.HasNonEmptyArray(root, "historyEvents") || (history is not null && ParkGraphUpsertProcessor.HasNonEmptyArray(history.Value, "events"));
    }

    internal async Task SaveHistoryAsync(ParkGraphUpsertRequest request, string? requestedByUserId, bool apply, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        ParkGraphUpsertHistoryEntry entry = new ParkGraphUpsertHistoryEntry
        {
            OperationKind = apply ? "apply" : "preview",
            TargetParkId = result.TargetParkId,
            TargetParkName = result.TargetParkName,
            RequestedByUserId = requestedByUserId,
            RawJson = request.RawJson,
            Result = result,
        };
        await this.historyRepository.SaveAsync(entry, cancellationToken);
    }
}
