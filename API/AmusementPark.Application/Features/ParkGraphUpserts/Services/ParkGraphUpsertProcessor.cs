using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
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
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphUpsertProcessor
{
    private readonly IParkRepository parkRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkFounderRepository parkFounderRepository;
    private readonly IParkOperatorRepository parkOperatorRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly IImageRepository imageRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly IParkGraphUpsertHistoryRepository historyRepository;

    public ParkGraphUpsertProcessor(
        IParkRepository parkRepository,
        IParkZoneRepository parkZoneRepository,
        IParkItemRepository parkItemRepository,
        IParkFounderRepository parkFounderRepository,
        IParkOperatorRepository parkOperatorRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        IImageRepository imageRepository,
        ISearchProjectionWriter searchProjectionWriter,
        IParkGraphUpsertHistoryRepository historyRepository)
    {
        this.parkRepository = parkRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.imageRepository = imageRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.historyRepository = historyRepository;
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
        }

        result.TargetParkId = targetPark.Id;
        result.TargetParkName = targetPark.Name;

        Dictionary<string, string> zoneKeys = await this.ProcessZonesAsync(root, targetPark, result, apply, cancellationToken);
        Dictionary<string, string> itemKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        List<string> changedItemIds = await this.ProcessItemsAsync(root, targetPark, zoneKeys, manufacturerKeys, itemKeys, result, apply, cancellationToken);
        await this.ProcessImagesAsync(root, targetPark, itemKeys, result, apply, cancellationToken);

        if (apply)
        {
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, targetPark.Id, cancellationToken);
            if (changedItemIds.Count > 0)
            {
                await this.searchProjectionWriter.UpsertManyAsync(SearchProjectionResourceTypes.ParkItems, changedItemIds, cancellationToken);
            }
        }

        FinalizeCounts(result);
        await this.SaveHistoryAsync(request, requestedByUserId, apply, result, cancellationToken);
        return ApplicationResult<ParkGraphUpsertResult>.Success(result);
    }

    private async Task ProcessFoundersAsync(JsonElement references, Dictionary<string, string> founderKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!references.TryGetProperty("founders", out JsonElement founders) || founders.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        IReadOnlyCollection<ParkFounder> existingFounders = await this.parkFounderRepository.GetAllAsync(cancellationToken);
        foreach (JsonElement patch in founders.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ReadString(patch, "key");
            string? id = ReadString(patch, "id");
            string? name = ReadString(patch, "name");
            ParkFounder? entity = FindByIdOrName(existingFounders, id, name, static value => value.Id, static value => value.Name);
            bool isNew = entity is null;
            entity ??= new ParkFounder { Name = name ?? string.Empty };
            ParkGraphUpsertChange change = BuildEntityChange("ParkFounder", entity.Id, key, entity.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : MatchMode(id, name));
            PatchFounder(entity, patch, change);

            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                entity = isNew
                    ? await this.parkFounderRepository.CreateAsync(entity, cancellationToken)
                    : await this.parkFounderRepository.UpdateAsync(entity.Id, entity, cancellationToken) ?? entity;
                await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Founders, entity.Id, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                founderKeys[key] = entity.Id;
            }

            result.Changes.Add(change);
        }
    }

    private async Task ProcessOperatorsAsync(JsonElement references, Dictionary<string, string> operatorKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!references.TryGetProperty("operators", out JsonElement operators) || operators.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        IReadOnlyCollection<ParkOperator> existingOperators = await this.parkOperatorRepository.GetAllAsync(cancellationToken);
        foreach (JsonElement patch in operators.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ReadString(patch, "key");
            string? id = ReadString(patch, "id");
            string? name = ReadString(patch, "name");
            ParkOperator? entity = FindByIdOrName(existingOperators, id, name, static value => value.Id, static value => value.Name);
            bool isNew = entity is null;
            entity ??= new ParkOperator { Name = name ?? string.Empty };
            ParkGraphUpsertChange change = BuildEntityChange("ParkOperator", entity.Id, key, entity.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : MatchMode(id, name));
            PatchOperator(entity, patch, change);

            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                entity = isNew
                    ? await this.parkOperatorRepository.CreateAsync(entity, cancellationToken)
                    : await this.parkOperatorRepository.UpdateAsync(entity.Id, entity, cancellationToken) ?? entity;
                await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Operators, entity.Id, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                operatorKeys[key] = entity.Id;
            }

            result.Changes.Add(change);
        }
    }

    private async Task ProcessManufacturersAsync(JsonElement references, Dictionary<string, string> manufacturerKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!references.TryGetProperty("manufacturers", out JsonElement manufacturers) || manufacturers.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        IReadOnlyCollection<AttractionManufacturer> existingManufacturers = await this.attractionManufacturerRepository.GetAllAsync(cancellationToken);
        foreach (JsonElement patch in manufacturers.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ReadString(patch, "key");
            string? id = ReadString(patch, "id");
            string? name = ReadString(patch, "name");
            AttractionManufacturer? entity = FindByIdOrName(existingManufacturers, id, name, static value => value.Id, static value => value.Name);
            bool isNew = entity is null;
            entity ??= new AttractionManufacturer { Name = name ?? string.Empty };
            ParkGraphUpsertChange change = BuildEntityChange("AttractionManufacturer", entity.Id, key, entity.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : MatchMode(id, name));
            PatchManufacturer(entity, patch, change);

            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                entity = isNew
                    ? await this.attractionManufacturerRepository.CreateAsync(entity, cancellationToken)
                    : await this.attractionManufacturerRepository.UpdateAsync(entity.Id, entity, cancellationToken) ?? entity;
                await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, entity.Id, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                manufacturerKeys[key] = entity.Id;
            }

            result.Changes.Add(change);
        }
    }

    private async Task<Dictionary<string, string>> ProcessZonesAsync(JsonElement root, Park park, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        Dictionary<string, string> zoneKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("zones", out JsonElement zones) || zones.ValueKind != JsonValueKind.Array)
        {
            return zoneKeys;
        }

        IReadOnlyCollection<ParkZone> existingZones = await this.parkZoneRepository.GetByParkIdAsync(park.Id, cancellationToken);
        List<ParkZone> mutableZones = existingZones.ToList();
        foreach (JsonElement patch in zones.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ReadString(patch, "key");
            string? id = ReadString(patch, "id");
            string? name = ReadString(GetObject(patch, "identity"), "name") ?? ReadString(patch, "name");
            string? slug = ReadString(GetObject(patch, "identity"), "slug") ?? ReadString(patch, "slug");
            ParkZone? zone = FindZone(mutableZones, id, slug, name);
            bool isNew = zone is null;
            zone ??= new ParkZone { ParkId = park.Id, Name = name ?? string.Empty };

            ParkGraphUpsertChange change = BuildEntityChange("ParkZone", zone.Id, key, zone.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : MatchMode(id, name));
            PatchZone(zone, patch, change);
            zone.ParkId = park.Id;

            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                zone = isNew
                    ? await this.parkZoneRepository.CreateAsync(zone, cancellationToken)
                    : await this.parkZoneRepository.UpdateAsync(zone.Id, zone, cancellationToken) ?? zone;
            }

            if (isNew)
            {
                mutableZones.Add(zone);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                zoneKeys[key] = zone.Id;
            }

            if (!string.IsNullOrWhiteSpace(zone.Name))
            {
                zoneKeys[$"zone:{NormalizeKey(zone.Name)}"] = zone.Id;
            }

            result.Changes.Add(change);
        }

        return zoneKeys;
    }

    private async Task<List<string>> ProcessItemsAsync(JsonElement root, Park park, Dictionary<string, string> zoneKeys, Dictionary<string, string> manufacturerKeys, Dictionary<string, string> itemKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        List<string> changedIds = new List<string>();
        if (!root.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array)
        {
            return changedIds;
        }

        IReadOnlyCollection<ParkItem> existingItems = await this.parkItemRepository.GetByParkIdAsync(park.Id, true, cancellationToken);
        List<ParkItem> mutableItems = existingItems.ToList();
        foreach (JsonElement patch in items.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ReadString(patch, "key");
            string? id = ReadString(patch, "id");
            string? name = ReadString(GetObject(patch, "identity"), "name") ?? ReadString(patch, "name");
            string? externalSource = ReadString(GetObject(patch, "identity"), "externalSource") ?? ReadString(GetObject(patch, "attractionDetails"), "externalSource");
            string? externalId = ReadString(GetObject(patch, "identity"), "externalId") ?? ReadString(GetObject(patch, "attractionDetails"), "externalId");
            ParkItem? item = FindItem(mutableItems, id, name, externalSource, externalId);
            bool isNew = item is null;
            item ??= new ParkItem
            {
                ParkId = park.Id,
                Name = name ?? string.Empty,
                Category = ParkItemCategory.Attraction,
                Type = ParkItemType.Attraction,
                IsVisible = true,
                AdminReviewStatus = AdminReviewStatus.ToReview,
            };

            ParkGraphUpsertChange change = BuildEntityChange("ParkItem", item.Id, key, item.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : MatchMode(id, name));
            PatchItem(item, patch, zoneKeys, manufacturerKeys, change, result, isNew);
            item.ParkId = park.Id;

            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                item = isNew
                    ? await this.parkItemRepository.CreateAsync(item, cancellationToken)
                    : await this.parkItemRepository.UpdateAsync(item.Id, item, cancellationToken) ?? item;
                changedIds.Add(item.Id);
            }
            else if (!apply && (change.Fields.Count > 0 || isNew))
            {
                changedIds.Add(item.Id);
            }

            if (isNew)
            {
                mutableItems.Add(item);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                itemKeys[key] = item.Id;
            }

            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                itemKeys[$"item:{NormalizeKey(item.Name)}"] = item.Id;
            }

            result.Changes.Add(change);
        }

        return changedIds;
    }

    private async Task ProcessImagesAsync(JsonElement root, Park park, Dictionary<string, string> itemKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("images", out JsonElement images) || images.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement patch in images.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? imageId = ReadString(patch, "imageId") ?? ReadString(patch, "id");
            if (string.IsNullOrWhiteSpace(imageId))
            {
                result.Warnings.Add("Image ignorée : imageId manquant. Le binaire reste géré par l'upload multipart existant.");
                continue;
            }

            Image? image = await this.imageRepository.GetByIdAsync(imageId, cancellationToken);
            if (image is null)
            {
                result.Warnings.Add($"Image '{imageId}' introuvable : rattachement ignoré.");
                continue;
            }

            string? ownerTypeText = ReadString(patch, "ownerType");
            string? ownerId = ReadString(patch, "ownerId");
            ResolveImageOwner(patch, park, itemKeys, ownerTypeText, ownerId, out ImageOwnerType ownerType, out string? resolvedOwnerId);

            ParkGraphUpsertChange change = BuildEntityChange("Image", image.Id, null, image.OriginalFileName ?? image.Id, "Updated", "imageId");
            AddChange(change, "ownerType", image.OwnerType.ToString(), ownerType.ToString());
            AddChange(change, "ownerId", image.OwnerId, resolvedOwnerId);

            if (apply && resolvedOwnerId is not null)
            {
                Image? linked = await this.imageRepository.LinkAsync(image.Id, ownerType, resolvedOwnerId, cancellationToken);
                image = linked ?? image;
            }

            ImageMetadataUpdate metadata = new ImageMetadataUpdate
            {
                Description = HasProperty(patch, "description") ? ReadStringAllowNull(patch, "description") : image.Description,
                AltTexts = HasProperty(patch, "altTexts") ? ToLocalizedTextValues(MergeLocalizedTexts(image.AltTexts, GetArray(patch, "altTexts"), false)) : ToLocalizedTextValues(image.AltTexts),
                Captions = HasProperty(patch, "captions") ? ToLocalizedTextValues(MergeLocalizedTexts(image.Captions, GetArray(patch, "captions"), false)) : ToLocalizedTextValues(image.Captions),
                Credits = HasProperty(patch, "credits") ? ToLocalizedTextValues(MergeLocalizedTexts(image.Credits, GetArray(patch, "credits"), false)) : ToLocalizedTextValues(image.Credits),
                TagIds = HasProperty(patch, "tagIds") ? ReadStringArray(GetArray(patch, "tagIds")) : image.TagIds,
                GeoLocation = image.GeoLocation is null ? null : new GeoPointValue(image.GeoLocation.Latitude, image.GeoLocation.Longitude),
                Category = image.Category,
                IsPublished = HasProperty(patch, "isPublished") ? ReadBool(patch, "isPublished") ?? image.IsPublished : image.IsPublished,
            };

            if (apply)
            {
                await this.imageRepository.UpdateMetadataAsync(image.Id, metadata, cancellationToken);
                if (ReadBool(patch, "setAsCurrent") == true && resolvedOwnerId is not null)
                {
                    await this.imageRepository.SetCurrentAsync(image.Id, ownerType, resolvedOwnerId, cancellationToken);
                }
            }

            result.Changes.Add(change);
        }
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

    private static Park BuildNewParkFromPatch(JsonElement? parkPatch, JsonElement? identity, ParkGraphUpsertResult result)
    {
        Park park = new Park
        {
            Name = ReadString(parkPatch, "name") ?? ReadString(identity, "name"),
            CountryCode = ReadString(parkPatch, "countryCode") ?? ReadString(identity, "countryCode"),
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        double? latitude = ReadDouble(parkPatch, "latitude");
        double? longitude = ReadDouble(parkPatch, "longitude");
        if (latitude.HasValue && longitude.HasValue)
        {
            park.SetPosition(latitude.Value, longitude.Value);
        }
        else
        {
            result.Warnings.Add("Création de parc demandée sans latitude/longitude complètes : coordonnées non définies.");
        }

        return park;
    }

    private static void PatchPark(Park park, JsonElement? patch, JsonElement? identity, Dictionary<string, string> founderKeys, Dictionary<string, string> operatorKeys, ParkGraphUpsertChange change, ParkGraphUpsertResult result, bool isNew)
    {
        if (patch is null)
        {
            return;
        }

        PatchString(patch, "name", park.Name, value => park.Name = value, change);
        PatchString(patch, "countryCode", park.CountryCode, value => park.CountryCode = value?.ToUpperInvariant(), change);
        PatchEnumNullable(patch, "type", park.Type, value => park.Type = value, change, "type");
        PatchString(patch, "founderId", park.FounderId, value => park.FounderId = value, change);
        PatchString(patch, "operatorId", park.OperatorId, value => park.OperatorId = value, change);
        PatchString(patch, "websiteUrl", park.WebsiteUrl, value => park.WebsiteUrl = value, change);
        PatchString(patch, "street", park.Street, value => park.Street = value, change);
        PatchString(patch, "city", park.City, value => park.City = value, change);
        PatchString(patch, "postalCode", park.PostalCode, value => park.PostalCode = value, change);
        PatchBool(patch, "isVisible", park.IsVisible, value => park.IsVisible = value, change);
        PatchBool(patch, "isFeaturedOnHome", park.IsFeaturedOnHome, value => park.IsFeaturedOnHome = value, change);
        PatchBool(patch, "isFeaturedOnHomeSponsored", park.IsFeaturedOnHomeSponsored, value => park.IsFeaturedOnHomeSponsored = value, change);
        PatchIntNullable(patch, "featuredHomeOrder", park.FeaturedHomeOrder, value => park.FeaturedHomeOrder = value, change);
        PatchEnum(patch, "adminReviewStatus", park.AdminReviewStatus, value => park.AdminReviewStatus = value, change);

        string? founderKey = ReadString(patch, "founderKey");
        if (!string.IsNullOrWhiteSpace(founderKey) && founderKeys.TryGetValue(founderKey, out string? founderId))
        {
            AddChange(change, "founderId", park.FounderId, founderId);
            park.FounderId = founderId;
        }

        string? operatorKey = ReadString(patch, "operatorKey");
        if (!string.IsNullOrWhiteSpace(operatorKey) && operatorKeys.TryGetValue(operatorKey, out string? operatorId))
        {
            AddChange(change, "operatorId", park.OperatorId, operatorId);
            park.OperatorId = operatorId;
        }

        if (HasProperty(patch, "descriptions"))
        {
            List<LocalizedText> merged = MergeLocalizedTexts(park.Descriptions, GetArray(patch, "descriptions"), false);
            AddChange(change, "descriptions", DescribeLocalized(park.Descriptions), DescribeLocalized(merged));
            park.Descriptions = merged;
        }

        bool hasLatitude = HasProperty(patch, "latitude");
        bool hasLongitude = HasProperty(patch, "longitude");
        if (hasLatitude || hasLongitude)
        {
            double? latitude = ReadDouble(patch, "latitude") ?? park.Position?.Latitude;
            double? longitude = ReadDouble(patch, "longitude") ?? park.Position?.Longitude;
            if (latitude.HasValue && longitude.HasValue)
            {
                AddChange(change, "position", FormatPosition(park.Position), $"{latitude.Value.ToString(CultureInfo.InvariantCulture)},{longitude.Value.ToString(CultureInfo.InvariantCulture)}");
                park.SetPosition(latitude.Value, longitude.Value);
            }
            else if (isNew)
            {
                result.Warnings.Add("Le parc créé n'a pas de coordonnées complètes.");
            }
        }
    }

    private static void PatchZone(ParkZone zone, JsonElement patch, ParkGraphUpsertChange change)
    {
        PatchString(patch, "name", zone.Name, value => zone.Name = value ?? string.Empty, change);
        PatchString(patch, "slug", zone.Slug, value => zone.Slug = value, change);
        PatchBool(patch, "isVisible", zone.IsVisible, value => zone.IsVisible = value, change);
        PatchInt(patch, "sortOrder", zone.SortOrder, value => zone.SortOrder = value, change);

        if (HasProperty(patch, "names"))
        {
            List<LocalizedText> merged = MergeLocalizedTexts(zone.Names, GetArray(patch, "names"), false);
            AddChange(change, "names", DescribeLocalized(zone.Names), DescribeLocalized(merged));
            zone.Names = merged;
        }

        if (HasProperty(patch, "descriptions"))
        {
            List<LocalizedText> merged = MergeLocalizedTexts(zone.Descriptions, GetArray(patch, "descriptions"), false);
            AddChange(change, "descriptions", DescribeLocalized(zone.Descriptions), DescribeLocalized(merged));
            zone.Descriptions = merged;
        }

        ApplyOptionalPositionPatch(zone, patch, change);
    }

    private static void PatchItem(ParkItem item, JsonElement patch, Dictionary<string, string> zoneKeys, Dictionary<string, string> manufacturerKeys, ParkGraphUpsertChange change, ParkGraphUpsertResult result, bool isNew)
    {
        PatchString(patch, "name", item.Name, value => item.Name = value ?? string.Empty, change);
        PatchString(patch, "subtype", item.Subtype, value => item.Subtype = value, change);
        PatchEnum(patch, "category", item.Category, value => item.Category = value, change);
        PatchEnum(patch, "type", item.Type, value => item.Type = value, change);
        PatchBool(patch, "isVisible", item.IsVisible, value => item.IsVisible = value, change);
        PatchEnum(patch, "adminReviewStatus", item.AdminReviewStatus, value => item.AdminReviewStatus = value, change);

        string? zoneId = ReadString(patch, "zoneId");
        string? zoneKey = ReadString(patch, "zoneKey");
        if (!string.IsNullOrWhiteSpace(zoneKey) && zoneKeys.TryGetValue(zoneKey, out string? resolvedZoneId))
        {
            zoneId = resolvedZoneId;
        }
        else if (!string.IsNullOrWhiteSpace(zoneKey))
        {
            string normalizedZoneNameKey = $"zone:{NormalizeKey(zoneKey)}";
            if (zoneKeys.TryGetValue(normalizedZoneNameKey, out string? resolvedByName))
            {
                zoneId = resolvedByName;
            }
            else
            {
                result.Warnings.Add($"ZoneKey '{zoneKey}' non résolue pour l'élément '{item.Name}'.");
            }
        }

        if (HasProperty(patch, "zoneId") || HasProperty(patch, "zoneKey"))
        {
            AddChange(change, "zoneId", item.ZoneId, zoneId);
            item.ZoneId = zoneId;
        }

        if (HasProperty(patch, "descriptions"))
        {
            List<LocalizedText> merged = MergeLocalizedTexts(item.Descriptions, GetArray(patch, "descriptions"), false);
            AddChange(change, "descriptions", DescribeLocalized(item.Descriptions), DescribeLocalized(merged));
            item.Descriptions = merged;
        }

        ApplyOptionalPositionPatch(item, patch, change);

        if (HasProperty(patch, "attractionDetails"))
        {
            JsonElement? detailsPatch = GetObject(patch, "attractionDetails");
            item.AttractionDetails ??= new AttractionDetails();
            PatchAttractionDetails(item.AttractionDetails, detailsPatch, manufacturerKeys, change, result, item.Name);
        }
        else if (isNew && item.Category == ParkItemCategory.Attraction)
        {
            item.AttractionDetails ??= new AttractionDetails();
        }

        if (HasProperty(patch, "attractionLocations"))
        {
            item.AttractionLocations ??= new AttractionLocations();
            PatchAttractionLocations(item.AttractionLocations, GetObject(patch, "attractionLocations"), change);
        }
    }

    private static void PatchAttractionDetails(AttractionDetails details, JsonElement? patch, Dictionary<string, string> manufacturerKeys, ParkGraphUpsertChange change, ParkGraphUpsertResult result, string itemName)
    {
        if (patch is null)
        {
            return;
        }

        PatchString(patch, "manufacturerId", details.ManufacturerId, value => details.ManufacturerId = value, change, "attractionDetails.manufacturerId");
        string? manufacturerKey = ReadString(patch, "manufacturerKey");
        if (!string.IsNullOrWhiteSpace(manufacturerKey) && manufacturerKeys.TryGetValue(manufacturerKey, out string? manufacturerId))
        {
            AddChange(change, "attractionDetails.manufacturerId", details.ManufacturerId, manufacturerId);
            details.ManufacturerId = manufacturerId;
        }
        else if (!string.IsNullOrWhiteSpace(manufacturerKey))
        {
            result.Warnings.Add($"ManufacturerKey '{manufacturerKey}' non résolue pour '{itemName}'.");
        }

        PatchString(patch, "model", details.Model, value => details.Model = value, change, "attractionDetails.model");
        PatchString(patch, "externalSource", details.ExternalSource, value => details.ExternalSource = value, change, "attractionDetails.externalSource");
        PatchString(patch, "externalId", details.ExternalId, value => details.ExternalId = value, change, "attractionDetails.externalId");
        PatchString(patch, "sourceUrl", details.SourceUrl, value => details.SourceUrl = value, change, "attractionDetails.sourceUrl");
        PatchString(patch, "status", details.Status, value => details.Status = value, change, "attractionDetails.status");
        PatchString(patch, "materialType", details.MaterialType, value => details.MaterialType = value, change, "attractionDetails.materialType");
        PatchString(patch, "seatingType", details.SeatingType, value => details.SeatingType = value, change, "attractionDetails.seatingType");
        PatchString(patch, "launchType", details.LaunchType, value => details.LaunchType = value, change, "attractionDetails.launchType");
        PatchString(patch, "restraintType", details.RestraintType, value => details.RestraintType = value, change, "attractionDetails.restraintType");
        PatchBoolNullable(patch, "isLaunched", details.IsLaunched, value => details.IsLaunched = value, change, "attractionDetails.isLaunched");
        PatchDateNullable(patch, "openingDate", details.OpeningDate, value => details.OpeningDate = value, change, "attractionDetails.openingDate");
        PatchDateNullable(patch, "closingDate", details.ClosingDate, value => details.ClosingDate = value, change, "attractionDetails.closingDate");
        PatchString(patch, "openingDateText", details.OpeningDateText, value => details.OpeningDateText = value, change, "attractionDetails.openingDateText");
        PatchString(patch, "closingDateText", details.ClosingDateText, value => details.ClosingDateText = value, change, "attractionDetails.closingDateText");
        PatchIntNullable(patch, "durationInSeconds", details.DurationInSeconds, value => details.DurationInSeconds = value, change, "attractionDetails.durationInSeconds");
        PatchIntNullable(patch, "capacityPerHour", details.CapacityPerHour, value => details.CapacityPerHour = value, change, "attractionDetails.capacityPerHour");
        PatchDoubleNullable(patch, "heightInFeet", details.HeightInFeet, value => details.HeightInFeet = value, change, "attractionDetails.heightInFeet");
        PatchDoubleNullable(patch, "heightInMeters", details.HeightInMeters, value => details.HeightInMeters = value, change, "attractionDetails.heightInMeters");
        PatchDoubleNullable(patch, "lengthInFeet", details.LengthInFeet, value => details.LengthInFeet = value, change, "attractionDetails.lengthInFeet");
        PatchDoubleNullable(patch, "lengthInMeters", details.LengthInMeters, value => details.LengthInMeters = value, change, "attractionDetails.lengthInMeters");
        PatchDoubleNullable(patch, "speedInMph", details.SpeedInMph, value => details.SpeedInMph = value, change, "attractionDetails.speedInMph");
        PatchDoubleNullable(patch, "speedInKmH", details.SpeedInKmH, value => details.SpeedInKmH = value, change, "attractionDetails.speedInKmH");
        PatchDoubleNullable(patch, "dropInMeters", details.DropInMeters, value => details.DropInMeters = value, change, "attractionDetails.dropInMeters");
        PatchIntNullable(patch, "inversionCount", details.InversionCount, value => details.InversionCount = value, change, "attractionDetails.inversionCount");
        PatchIntNullable(patch, "trainCount", details.TrainCount, value => details.TrainCount = value, change, "attractionDetails.trainCount");
        PatchIntNullable(patch, "carsPerTrain", details.CarsPerTrain, value => details.CarsPerTrain = value, change, "attractionDetails.carsPerTrain");
        PatchIntNullable(patch, "ridersPerVehicle", details.RidersPerVehicle, value => details.RidersPerVehicle = value, change, "attractionDetails.ridersPerVehicle");
        PatchBoolNullable(patch, "hasSingleRider", details.HasSingleRider, value => details.HasSingleRider = value, change, "attractionDetails.hasSingleRider");
        PatchBoolNullable(patch, "hasFastPass", details.HasFastPass, value => details.HasFastPass = value, change, "attractionDetails.hasFastPass");
        PatchBoolNullable(patch, "isAccessibleForReducedMobility", details.IsAccessibleForReducedMobility, value => details.IsAccessibleForReducedMobility = value, change, "attractionDetails.isAccessibleForReducedMobility");
        PatchBoolNullable(patch, "isIndoor", details.IsIndoor, value => details.IsIndoor = value, change, "attractionDetails.isIndoor");
        PatchEnumNullable(patch, "waterExposureLevel", details.WaterExposureLevel, value => details.WaterExposureLevel = value, change, "attractionDetails.waterExposureLevel");

        if (HasProperty(patch, "accessConditions"))
        {
            List<AttractionAccessCondition> conditions = ReadAccessConditions(GetArray(patch, "accessConditions"));
            AddChange(change, "attractionDetails.accessConditions", details.AccessConditions.Count.ToString(CultureInfo.InvariantCulture), conditions.Count.ToString(CultureInfo.InvariantCulture));
            details.AccessConditions = conditions;
        }
    }

    private static void PatchAttractionLocations(AttractionLocations locations, JsonElement? patch, ParkGraphUpsertChange change)
    {
        if (patch is null)
        {
            return;
        }

        PatchLocationPoint(patch, "entrance", locations.Entrance, value => locations.Entrance = value, change, "attractionLocations.entrance");
        PatchLocationPoint(patch, "exit", locations.Exit, value => locations.Exit = value, change, "attractionLocations.exit");
        PatchLocationPoint(patch, "fastPassEntrance", locations.FastPassEntrance, value => locations.FastPassEntrance = value, change, "attractionLocations.fastPassEntrance");
        PatchLocationPoint(patch, "reducedMobilityEntrance", locations.ReducedMobilityEntrance, value => locations.ReducedMobilityEntrance = value, change, "attractionLocations.reducedMobilityEntrance");
    }

    private static void PatchFounder(ParkFounder entity, JsonElement patch, ParkGraphUpsertChange change)
    {
        PatchString(patch, "name", entity.Name, value => entity.Name = value ?? string.Empty, change);
        PatchString(patch, "occupation", entity.Occupation, value => entity.Occupation = value, change);
        PatchString(patch, "birthDate", entity.BirthDate, value => entity.BirthDate = value, change);
        PatchString(patch, "deathDate", entity.DeathDate, value => entity.DeathDate = value, change);
        PatchString(patch, "birthPlace", entity.BirthPlace, value => entity.BirthPlace = value, change);
        PatchString(patch, "nationalityCountryCode", entity.NationalityCountryCode, value => entity.NationalityCountryCode = value?.ToUpperInvariant(), change);
        PatchString(patch, "websiteUrl", entity.WebsiteUrl, value => entity.WebsiteUrl = value, change);
        if (HasProperty(patch, "biography"))
        {
            List<LocalizedText> merged = MergeLocalizedTexts(entity.Biography, GetArray(patch, "biography"), false);
            AddChange(change, "biography", DescribeLocalized(entity.Biography), DescribeLocalized(merged));
            entity.Biography = merged;
        }
    }

    private static void PatchOperator(ParkOperator entity, JsonElement patch, ParkGraphUpsertChange change)
    {
        PatchString(patch, "name", entity.Name, value => entity.Name = value ?? string.Empty, change);
        PatchString(patch, "legalName", entity.LegalName, value => entity.LegalName = value, change);
        PatchIntNullable(patch, "foundedYear", entity.FoundedYear, value => entity.FoundedYear = value, change);
        PatchIntNullable(patch, "closedYear", entity.ClosedYear, value => entity.ClosedYear = value, change);
        PatchEnum(patch, "adminReviewStatus", entity.AdminReviewStatus, value => entity.AdminReviewStatus = value, change);
        PatchContactDetails(patch, entity.ContactDetails, value => entity.ContactDetails = value, change);
        if (HasProperty(patch, "description"))
        {
            List<LocalizedText> merged = MergeLocalizedTexts(entity.Description, GetArray(patch, "description"), false);
            AddChange(change, "description", DescribeLocalized(entity.Description), DescribeLocalized(merged));
            entity.Description = merged;
        }
    }

    private static void PatchManufacturer(AttractionManufacturer entity, JsonElement patch, ParkGraphUpsertChange change)
    {
        PatchString(patch, "name", entity.Name, value => entity.Name = value ?? string.Empty, change);
        PatchString(patch, "legalName", entity.LegalName, value => entity.LegalName = value, change);
        PatchIntNullable(patch, "foundedYear", entity.FoundedYear, value => entity.FoundedYear = value, change);
        PatchIntNullable(patch, "closedYear", entity.ClosedYear, value => entity.ClosedYear = value, change);
        PatchEnum(patch, "adminReviewStatus", entity.AdminReviewStatus, value => entity.AdminReviewStatus = value, change);
        PatchContactDetails(patch, entity.ContactDetails, value => entity.ContactDetails = value, change);
        if (HasProperty(patch, "biography"))
        {
            List<LocalizedText> merged = MergeLocalizedTexts(entity.Biography, GetArray(patch, "biography"), false);
            AddChange(change, "biography", DescribeLocalized(entity.Biography), DescribeLocalized(merged));
            entity.Biography = merged;
        }
    }


    private static void PatchContactDetails(JsonElement patch, ParkReferenceContactDetails? current, Action<ParkReferenceContactDetails?> assign, ParkGraphUpsertChange change)
    {
        if (!HasProperty(patch, "contactDetails"))
        {
            return;
        }

        if (HasNull(patch, "contactDetails"))
        {
            AddChange(change, "contactDetails", DescribeContactDetails(current), null);
            assign(null);
            return;
        }

        JsonElement? contactPatch = GetObject(patch, "contactDetails");
        if (contactPatch is null)
        {
            return;
        }

        ParkReferenceContactDetails next = current is null
            ? new ParkReferenceContactDetails()
            : new ParkReferenceContactDetails
            {
                WebsiteUrl = current.WebsiteUrl,
                Email = current.Email,
                PhoneNumber = current.PhoneNumber,
                Street = current.Street,
                City = current.City,
                PostalCode = current.PostalCode,
                CountryCode = current.CountryCode,
                Latitude = current.Latitude,
                Longitude = current.Longitude,
            };

        string? before = DescribeContactDetails(current);
        if (HasProperty(contactPatch, "websiteUrl"))
        {
            next.WebsiteUrl = ReadStringAllowNull(contactPatch, "websiteUrl")?.Trim();
        }

        if (HasProperty(contactPatch, "email"))
        {
            next.Email = ReadStringAllowNull(contactPatch, "email")?.Trim();
        }

        if (HasProperty(contactPatch, "phoneNumber"))
        {
            next.PhoneNumber = ReadStringAllowNull(contactPatch, "phoneNumber")?.Trim();
        }

        if (HasProperty(contactPatch, "street"))
        {
            next.Street = ReadStringAllowNull(contactPatch, "street")?.Trim();
        }

        if (HasProperty(contactPatch, "city"))
        {
            next.City = ReadStringAllowNull(contactPatch, "city")?.Trim();
        }

        if (HasProperty(contactPatch, "postalCode"))
        {
            next.PostalCode = ReadStringAllowNull(contactPatch, "postalCode")?.Trim();
        }

        if (HasProperty(contactPatch, "countryCode"))
        {
            next.CountryCode = ReadString(contactPatch, "countryCode")?.ToUpperInvariant();
        }

        if (HasProperty(contactPatch, "latitude"))
        {
            next.Latitude = ReadDouble(contactPatch, "latitude");
        }

        if (HasProperty(contactPatch, "longitude"))
        {
            next.Longitude = ReadDouble(contactPatch, "longitude");
        }

        AddChange(change, "contactDetails", before, DescribeContactDetails(next));
        assign(next);
    }

    private static string? DescribeContactDetails(ParkReferenceContactDetails? contactDetails)
    {
        if (contactDetails is null)
        {
            return null;
        }

        return string.Join(" | ", new[]
        {
            contactDetails.WebsiteUrl,
            contactDetails.Email,
            contactDetails.PhoneNumber,
            contactDetails.Street,
            contactDetails.City,
            contactDetails.PostalCode,
            contactDetails.CountryCode,
            contactDetails.Latitude?.ToString(CultureInfo.InvariantCulture),
            contactDetails.Longitude?.ToString(CultureInfo.InvariantCulture),
        }.Where(static value => !string.IsNullOrWhiteSpace(value)));
    }

    private static void ApplyOptionalPositionPatch(AmusementPark.Core.Geo.GeolocatedEntityBase entity, JsonElement patch, ParkGraphUpsertChange change)
    {
        bool hasLatitude = HasProperty(patch, "latitude");
        bool hasLongitude = HasProperty(patch, "longitude");
        if (!hasLatitude && !hasLongitude)
        {
            return;
        }

        double? latitude = ReadDouble(patch, "latitude") ?? entity.Position?.Latitude;
        double? longitude = ReadDouble(patch, "longitude") ?? entity.Position?.Longitude;
        if (latitude.HasValue && longitude.HasValue)
        {
            AddChange(change, "position", FormatPosition(entity.Position), $"{latitude.Value.ToString(CultureInfo.InvariantCulture)},{longitude.Value.ToString(CultureInfo.InvariantCulture)}");
            entity.SetPosition(latitude.Value, longitude.Value);
        }
        else if (HasNull(patch, "latitude") || HasNull(patch, "longitude"))
        {
            AddChange(change, "position", FormatPosition(entity.Position), null);
            entity.ClearPosition();
        }
    }

    private static List<AttractionAccessCondition> ReadAccessConditions(JsonElement? array)
    {
        if (array is null || array.Value.ValueKind != JsonValueKind.Array)
        {
            return new List<AttractionAccessCondition>();
        }

        List<AttractionAccessCondition> conditions = new List<AttractionAccessCondition>();
        foreach (JsonElement item in array.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            AttractionAccessCondition condition = new AttractionAccessCondition
            {
                Type = ReadEnum(item, "type", AttractionAccessConditionType.Custom),
                TypeKey = ReadString(item, "typeKey"),
                IsCustom = ReadBool(item, "isCustom"),
                CustomTypeKey = ReadString(item, "customTypeKey"),
                CustomTypeLabel = ReadLocalizedTexts(GetArray(item, "customTypeLabel")),
                Value = ReadDouble(item, "value"),
                Unit = ReadEnumNullable<AttractionAccessConditionUnit>(item, "unit"),
                RequiresAccompaniment = ReadBool(item, "requiresAccompaniment"),
                MinimumCompanionAge = ReadInt(item, "minimumCompanionAge"),
                Label = ReadLocalizedTexts(GetArray(item, "label")),
                Description = ReadLocalizedTexts(GetArray(item, "description")),
                DisplayOrder = ReadInt(item, "displayOrder"),
            };
            conditions.Add(condition);
        }

        return conditions;
    }

    private static void ResolveImageOwner(JsonElement patch, Park park, Dictionary<string, string> itemKeys, string? ownerTypeText, string? ownerId, out ImageOwnerType ownerType, out string? resolvedOwnerId)
    {
        ownerType = ReadEnumFromText(ownerTypeText, ImageOwnerType.Park);
        resolvedOwnerId = NormalizeString(ownerId);
        string? ownerKey = ReadString(patch, "ownerKey");
        if (string.Equals(ownerKey, "park", StringComparison.OrdinalIgnoreCase))
        {
            ownerType = ImageOwnerType.Park;
            resolvedOwnerId = park.Id;
            return;
        }

        if (!string.IsNullOrWhiteSpace(ownerKey) && itemKeys.TryGetValue(ownerKey, out string? itemId))
        {
            ownerType = ImageOwnerType.Attraction;
            resolvedOwnerId = itemId;
            return;
        }

        if (string.IsNullOrWhiteSpace(ownerKey) == false)
        {
            string normalizedItemNameKey = $"item:{NormalizeKey(ownerKey)}";
            if (itemKeys.TryGetValue(normalizedItemNameKey, out string? itemIdByName))
            {
                ownerType = ImageOwnerType.Attraction;
                resolvedOwnerId = itemIdByName;
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(resolvedOwnerId))
        {
            resolvedOwnerId = park.Id;
            ownerType = ImageOwnerType.Park;
        }
    }

    private static T? FindByIdOrName<T>(IReadOnlyCollection<T> entities, string? id, string? name, Func<T, string> idSelector, Func<T, string> nameSelector)
        where T : class
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            T? byId = entities.FirstOrDefault(entity => string.Equals(idSelector(entity), id, StringComparison.Ordinal));
            if (byId is not null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            string normalizedName = NormalizeKey(name);
            return entities.FirstOrDefault(entity => string.Equals(NormalizeKey(nameSelector(entity)), normalizedName, StringComparison.OrdinalIgnoreCase));
        }

        return default;
    }

    private static ParkZone? FindZone(IReadOnlyCollection<ParkZone> zones, string? id, string? slug, string? name)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            ParkZone? byId = zones.FirstOrDefault(zone => string.Equals(zone.Id, id, StringComparison.Ordinal));
            if (byId is not null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(slug))
        {
            ParkZone? bySlug = zones.FirstOrDefault(zone => string.Equals(zone.Slug, slug, StringComparison.OrdinalIgnoreCase));
            if (bySlug is not null)
            {
                return bySlug;
            }
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            string normalizedName = NormalizeKey(name);
            return zones.FirstOrDefault(zone => string.Equals(NormalizeKey(zone.Name), normalizedName, StringComparison.OrdinalIgnoreCase)
                || zone.Names.Any(localized => string.Equals(NormalizeKey(localized.Value), normalizedName, StringComparison.OrdinalIgnoreCase)));
        }

        return null;
    }

    private static ParkItem? FindItem(IReadOnlyCollection<ParkItem> items, string? id, string? name, string? externalSource, string? externalId)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            ParkItem? byId = items.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal));
            if (byId is not null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(externalSource) && !string.IsNullOrWhiteSpace(externalId))
        {
            ParkItem? byExternalId = items.FirstOrDefault(item => string.Equals(item.AttractionDetails?.ExternalSource, externalSource, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.AttractionDetails?.ExternalId, externalId, StringComparison.OrdinalIgnoreCase));
            if (byExternalId is not null)
            {
                return byExternalId;
            }
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            string normalizedName = NormalizeKey(name);
            List<ParkItem> matches = items
                .Where(item => string.Equals(NormalizeKey(item.Name), normalizedName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count == 1)
            {
                return matches[0];
            }
        }

        return null;
    }

    private static ParkGraphUpsertChange BuildEntityChange(string entityType, string? entityId, string? entityKey, string displayName, string changeType, string matchedBy)
    {
        return new ParkGraphUpsertChange
        {
            EntityType = entityType,
            EntityId = entityId,
            EntityKey = entityKey,
            DisplayName = displayName,
            ChangeType = changeType,
            MatchedBy = matchedBy,
        };
    }

    private static void AddChange(ParkGraphUpsertChange change, string field, object? oldValue, object? newValue)
    {
        string? oldText = FormatValue(oldValue);
        string? newText = FormatValue(newValue);
        if (string.Equals(oldText, newText, StringComparison.Ordinal))
        {
            return;
        }

        change.Fields.Add(new ParkGraphUpsertFieldChange
        {
            Field = field,
            OldValue = oldText,
            NewValue = newText,
        });
    }

    private static void FinalizeCounts(ParkGraphUpsertResult result)
    {
        result.Counts.Created = result.Changes.Count(change => string.Equals(change.ChangeType, "Created", StringComparison.Ordinal));
        result.Counts.Updated = result.Changes.Count(change => string.Equals(change.ChangeType, "Updated", StringComparison.Ordinal));
        result.Counts.Unchanged = result.Changes.Count(change => string.Equals(change.ChangeType, "Unchanged", StringComparison.Ordinal));
        result.Counts.Warnings = result.Warnings.Count;
        result.Counts.Errors = result.Errors.Count;
        result.CanApply = result.Errors.Count == 0;
    }

    private static string MatchMode(string? id, string? name)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            return "id";
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            return "name";
        }

        return "none";
    }

    private static bool HasProperty(JsonElement? element, string propertyName)
    {
        return element is not null && element.Value.ValueKind == JsonValueKind.Object && element.Value.TryGetProperty(propertyName, out _);
    }

    private static bool HasNull(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property))
        {
            return false;
        }

        return property.ValueKind == JsonValueKind.Null;
    }

    private static JsonElement? GetObject(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return property;
    }

    private static JsonElement? GetArray(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return property;
    }

    private static string? ReadString(JsonElement? element, string propertyName)
    {
        return NormalizeString(ReadStringAllowNull(element, propertyName));
    }

    private static string? ReadStringAllowNull(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return property.ToString();
    }

    private static string? NormalizeString(string? value)
    {
        string? trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static bool? ReadBool(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (property.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out bool value))
        {
            return value;
        }

        return null;
    }

    private static int? ReadInt(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        return null;
    }

    private static double? ReadDouble(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out double number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String && double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return parsed;
        }

        return null;
    }

    private static DateTime? ReadDate(JsonElement? element, string propertyName)
    {
        string? value = ReadString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsed)
            ? parsed
            : null;
    }

    private static T ReadEnum<T>(JsonElement? element, string propertyName, T fallback)
        where T : struct, Enum
    {
        T? value = ReadEnumNullable<T>(element, propertyName);
        return value ?? fallback;
    }

    private static T? ReadEnumNullable<T>(JsonElement? element, string propertyName)
        where T : struct, Enum
    {
        string? value = ReadString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return TryReadEnum(value, out T parsed) ? parsed : null;
    }

    private static T ReadEnumFromText<T>(string? value, T fallback)
        where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return TryReadEnum(value, out T parsed) ? parsed : fallback;
    }

    private static bool TryReadEnum<T>(string value, out T parsed)
        where T : struct, Enum
    {
        if (Enum.TryParse(value, true, out parsed))
        {
            return true;
        }

        string normalized = NormalizeEnumToken(value);
        foreach (string name in Enum.GetNames<T>())
        {
            if (string.Equals(NormalizeEnumToken(name), normalized, StringComparison.OrdinalIgnoreCase))
            {
                parsed = Enum.Parse<T>(name);
                return true;
            }
        }

        parsed = default;
        return false;
    }

    private static string NormalizeEnumToken(string value)
    {
        return value.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static void PatchString(JsonElement? patch, string propertyName, string? current, Action<string?> assign, ParkGraphUpsertChange change, string? fieldName = null)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        string? next = ReadStringAllowNull(patch, propertyName)?.Trim();
        if (string.IsNullOrWhiteSpace(next))
        {
            next = null;
        }

        AddChange(change, fieldName ?? propertyName, current, next);
        assign(next);
    }

    private static void PatchBool(JsonElement? patch, string propertyName, bool current, Action<bool> assign, ParkGraphUpsertChange change)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        bool? next = ReadBool(patch, propertyName);
        if (!next.HasValue)
        {
            return;
        }

        AddChange(change, propertyName, current, next.Value);
        assign(next.Value);
    }

    private static void PatchBoolNullable(JsonElement? patch, string propertyName, bool? current, Action<bool?> assign, ParkGraphUpsertChange change, string fieldName)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        bool? next = ReadBool(patch, propertyName);
        AddChange(change, fieldName, current, next);
        assign(next);
    }

    private static void PatchInt(JsonElement? patch, string propertyName, int current, Action<int> assign, ParkGraphUpsertChange change)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        int? next = ReadInt(patch, propertyName);
        if (!next.HasValue)
        {
            return;
        }

        AddChange(change, propertyName, current, next.Value);
        assign(next.Value);
    }

    private static void PatchIntNullable(JsonElement? patch, string propertyName, int? current, Action<int?> assign, ParkGraphUpsertChange change, string? fieldName = null)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        int? next = ReadInt(patch, propertyName);
        AddChange(change, fieldName ?? propertyName, current, next);
        assign(next);
    }

    private static void PatchDoubleNullable(JsonElement? patch, string propertyName, double? current, Action<double?> assign, ParkGraphUpsertChange change, string fieldName)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        double? next = ReadDouble(patch, propertyName);
        AddChange(change, fieldName, current, next);
        assign(next);
    }

    private static void PatchDateNullable(JsonElement? patch, string propertyName, DateTime? current, Action<DateTime?> assign, ParkGraphUpsertChange change, string fieldName)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        DateTime? next = ReadDate(patch, propertyName);
        AddChange(change, fieldName, current?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), next?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        assign(next);
    }

    private static void PatchEnum<T>(JsonElement? patch, string propertyName, T current, Action<T> assign, ParkGraphUpsertChange change)
        where T : struct, Enum
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        T? next = ReadEnumNullable<T>(patch, propertyName);
        if (!next.HasValue)
        {
            return;
        }

        AddChange(change, propertyName, current, next.Value);
        assign(next.Value);
    }

    private static void PatchEnumNullable<T>(JsonElement? patch, string propertyName, T? current, Action<T?> assign, ParkGraphUpsertChange change, string fieldName)
        where T : struct, Enum
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        T? next = ReadEnumNullable<T>(patch, propertyName);
        AddChange(change, fieldName, current, next);
        assign(next);
    }

    private static void PatchLocationPoint(JsonElement? patch, string propertyName, GeoPoint? current, Action<GeoPoint?> assign, ParkGraphUpsertChange change, string fieldName)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        JsonElement? point = GetObject(patch, propertyName);
        GeoPoint? next = null;
        if (point is not null)
        {
            double? latitude = ReadDouble(point, "latitude");
            double? longitude = ReadDouble(point, "longitude");
            if (latitude.HasValue && longitude.HasValue)
            {
                next = new GeoPoint(latitude.Value, longitude.Value);
            }
        }

        AddChange(change, fieldName, FormatPosition(current), FormatPosition(next));
        assign(next);
    }

    private static List<LocalizedText> MergeLocalizedTexts(IReadOnlyCollection<LocalizedText> current, JsonElement? array, bool replace)
    {
        if (array is null || array.Value.ValueKind != JsonValueKind.Array)
        {
            return replace ? new List<LocalizedText>() : current.ToList();
        }

        Dictionary<string, LocalizedText> values = replace
            ? new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase)
            : current
                .Where(static item => !string.IsNullOrWhiteSpace(item.LanguageCode))
                .ToDictionary(static item => item.LanguageCode.Trim().ToLowerInvariant(), static item => new LocalizedText(item.LanguageCode, item.Value), StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement item in array.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? languageCode = ReadString(item, "languageCode")?.ToLowerInvariant();
            string? value = ReadStringAllowNull(item, "value")?.Trim();
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                values.Remove(languageCode);
            }
            else
            {
                values[languageCode] = new LocalizedText(languageCode, value);
            }
        }

        return values.Values.ToList();
    }

    private static List<LocalizedText> ReadLocalizedTexts(JsonElement? array)
    {
        return MergeLocalizedTexts(Array.Empty<LocalizedText>(), array, true);
    }

    private static List<string> ReadStringArray(JsonElement? array)
    {
        if (array is null || array.Value.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        List<string> values = new List<string>();
        foreach (JsonElement item in array.Value.EnumerateArray())
        {
            string? value = item.ValueKind == JsonValueKind.String ? NormalizeString(item.GetString()) : NormalizeString(item.ToString());
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values.Distinct(StringComparer.Ordinal).ToList();
    }

    private static IReadOnlyCollection<LocalizedTextValue> ToLocalizedTextValues(IReadOnlyCollection<LocalizedText> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode) && !string.IsNullOrWhiteSpace(value.Value))
            .Select(static value => new LocalizedTextValue(value.LanguageCode, value.Value ?? string.Empty))
            .ToList();
    }

    private static string DescribeLocalized(IReadOnlyCollection<LocalizedText> texts)
    {
        return string.Join(", ", texts.Select(static text => text.LanguageCode).OrderBy(static value => value, StringComparer.OrdinalIgnoreCase));
    }

    private static string? FormatPosition(GeoPoint? point)
    {
        if (point is null)
        {
            return null;
        }

        return $"{point.Latitude.ToString(CultureInfo.InvariantCulture)},{point.Longitude.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string? FormatValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is bool boolValue)
        {
            return boolValue ? "true" : "false";
        }

        if (value is DateTime dateValue)
        {
            return dateValue.ToString("O", CultureInfo.InvariantCulture);
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture);
        }

        return value.ToString();
    }

    private static string NormalizeKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
