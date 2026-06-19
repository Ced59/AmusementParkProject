using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private readonly IParkRepository parkRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkFounderRepository parkFounderRepository;
    private readonly IParkOperatorRepository parkOperatorRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly IImageRepository imageRepository;
    private readonly IRemoteImageImporter remoteImageImporter;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly IParkGraphUpsertHistoryRepository historyRepository;
    private readonly IPublicSeoUpdateNotifier publicSeoUpdateNotifier;
    private readonly IMeasurementConversionService measurementConversionService;

    public ParkGraphUpsertProcessor(
        IParkRepository parkRepository,
        IParkZoneRepository parkZoneRepository,
        IParkItemRepository parkItemRepository,
        IParkFounderRepository parkFounderRepository,
        IParkOperatorRepository parkOperatorRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        IImageRepository imageRepository,
        IRemoteImageImporter remoteImageImporter,
        ISearchProjectionWriter searchProjectionWriter,
        IParkGraphUpsertHistoryRepository historyRepository,
        IPublicSeoUpdateNotifier publicSeoUpdateNotifier,
        IMeasurementConversionService measurementConversionService)
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
    }

    public async Task<ApplicationResult<ParkGraphUpsertResult>> PreviewAsync(ParkGraphUpsertRequest request, string? requestedByUserId, CancellationToken cancellationToken)
    {
        return await this.ProcessAsync(request, requestedByUserId, false, cancellationToken);
    }

    public async Task<ApplicationResult<ParkGraphUpsertResult>> ApplyAsync(ParkGraphUpsertRequest request, string? requestedByUserId, CancellationToken cancellationToken)
    {
        return await this.ProcessAsync(request, requestedByUserId, true, cancellationToken);
    }

    private async Task<ApplicationResult<ParkGraphUpsertResult>> ProcessAsync(ParkGraphUpsertRequest request, string? requestedByUserId, bool apply, CancellationToken cancellationToken)
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
        string mode = ReadString(root, "mode") ?? "merge";
        result.Mode = mode;
        if (request.ReplaceCollections)
        {
            result.Warnings.Add("replaceCollections est reçu mais reste non destructif dans cette version : aucune zone ou aucun item absent du JSON n’est supprimé automatiquement.");
        }

        JsonElement? parkPatch = GetObject(root, "park");
        JsonElement? identity = GetObject(root, "identity");
        string? targetParkId = NormalizeString(request.TargetParkId)
            ?? ReadString(identity, "parkId")
            ?? ReadString(identity, "id")
            ?? ReadString(parkPatch, "id");

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
        else if (request.CreateIfMissing)
        {
            targetPark = BuildNewParkFromPatch(parkPatch, identity, result);
            parkWillBeCreated = true;
        }
        else
        {
            result.Errors.Add("Aucun parc cible sélectionné. Sélectionner un parc existant ou activer la création explicite.");
        }

        if (targetPark is null)
        {
            result.CanApply = false;
            FinalizeCounts(result);
            await this.SaveHistoryAsync(request, requestedByUserId, apply, result, cancellationToken);
            return apply
                ? ApplicationResult<ParkGraphUpsertResult>.Failure(ParkGraphUpsertApplicationErrors.CannotApply("Le document ne peut pas être appliqué car aucun parc cible fiable n'a été résolu."))
            : ApplicationResult<ParkGraphUpsertResult>.Success(result);
        }

        PublicSeoParkSnapshot? previousParkSnapshot = PublicSeoParkSnapshot.FromPark(targetPark);

        Dictionary<string, string> founderKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> operatorKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> manufacturerKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("references", out JsonElement references) && references.ValueKind == JsonValueKind.Object)
        {
            await this.ProcessFoundersAsync(references, founderKeys, result, apply, cancellationToken);
            await this.ProcessOperatorsAsync(references, operatorKeys, result, apply, cancellationToken);
            await this.ProcessManufacturersAsync(references, manufacturerKeys, result, apply, cancellationToken);
        }

        ParkGraphUpsertChange parkChange = BuildEntityChange("Park", targetPark.Id, "park", targetPark.Name ?? "Parc", parkWillBeCreated ? "Created" : "Unchanged", parkWillBeCreated ? "createIfMissing" : "id");
        PatchPark(targetPark, parkPatch, identity, founderKeys, operatorKeys, parkChange, result, parkWillBeCreated);
        if (parkChange.Fields.Count > 0 || parkWillBeCreated)
        {
            parkChange.ChangeType = parkWillBeCreated ? "Created" : "Updated";
        }

        result.Changes.Add(parkChange);

        if (apply)
        {
            targetPark = parkWillBeCreated
                ? await this.parkRepository.CreateAsync(targetPark, cancellationToken)
                : await this.parkRepository.UpdateAsync(targetPark.Id, targetPark, cancellationToken) ?? targetPark;
            parkChange.EntityId = targetPark.Id;
        }

        result.TargetParkId = targetPark.Id;
        result.TargetParkName = targetPark.Name;

        Dictionary<string, string> zoneKeys = await this.ProcessZonesAsync(root, targetPark, result, apply, cancellationToken);
        Dictionary<string, string> itemKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ParkGraphUpsertItemSeoChanges itemSeoChanges = await this.ProcessItemsAsync(root, targetPark, zoneKeys, manufacturerKeys, itemKeys, result, apply, cancellationToken);
        await this.ProcessImagesAsync(root, targetPark, itemKeys, founderKeys, operatorKeys, manufacturerKeys, result, apply, cancellationToken);
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
                IReadOnlyCollection<PublicSeoParkSnapshot> previousParks = previousParkSnapshot is null
                    ? Array.Empty<PublicSeoParkSnapshot>()
                    : new[] { previousParkSnapshot };
                await this.publicSeoUpdateNotifier.NotifyAsync(
                    new PublicSeoUpdate
                    {
                        PreviousParks = previousParks,
                        CurrentParks = PublicSeoParkSnapshot.FromParks(new[] { targetPark }),
                        PreviousParkItems = itemSeoChanges.PreviousItems,
                        CurrentParkItems = itemSeoChanges.CurrentItems,
                        IncludeDiscoveryPages = true,
                    },
                    cancellationToken);
            }
        }

        FinalizeCounts(result);
        await this.SaveHistoryAsync(request, requestedByUserId, apply, result, cancellationToken);
        return ApplicationResult<ParkGraphUpsertResult>.Success(result);
    }













    private async Task SaveHistoryAsync(ParkGraphUpsertRequest request, string? requestedByUserId, bool apply, ParkGraphUpsertResult result, CancellationToken cancellationToken)
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
