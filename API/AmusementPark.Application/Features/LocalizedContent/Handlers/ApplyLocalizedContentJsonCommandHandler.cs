using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.LocalizedContent.Commands;
using AmusementPark.Application.Features.LocalizedContent.Results;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.LocalizedContent.Handlers;

/// <summary>
/// Handler d'application d'un JSON localisé sur une entité administrable.
/// </summary>
public sealed class ApplyLocalizedContentJsonCommandHandler : ICommandHandler<ApplyLocalizedContentJsonCommand, ApplicationResult<LocalizedContentApplyResult>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkOperatorRepository parkOperatorRepository;
    private readonly IParkFounderRepository parkFounderRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly IImageRepository imageRepository;
    private readonly IImageTagRepository imageTagRepository;
    private readonly IAttractionAccessConditionTypeDefinitionRepository accessConditionTypeDefinitionRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public ApplyLocalizedContentJsonCommandHandler(
        IParkRepository parkRepository,
        IParkZoneRepository parkZoneRepository,
        IParkItemRepository parkItemRepository,
        IParkOperatorRepository parkOperatorRepository,
        IParkFounderRepository parkFounderRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        IImageRepository imageRepository,
        IImageTagRepository imageTagRepository,
        IAttractionAccessConditionTypeDefinitionRepository accessConditionTypeDefinitionRepository,
        ISearchProjectionWriter searchProjectionWriter)
    {
        this.parkRepository = parkRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.imageRepository = imageRepository;
        this.imageTagRepository = imageTagRepository;
        this.accessConditionTypeDefinitionRepository = accessConditionTypeDefinitionRepository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<LocalizedContentApplyResult>> HandleAsync(ApplyLocalizedContentJsonCommand command, CancellationToken cancellationToken = default)
    {
        if (!LocalizedContentEntityTypeParser.TryParse(command.EntityType, out LocalizedContentEntityType entityType))
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(LocalizedContentApplicationErrors.InvalidEntityType(command.EntityType));
        }

        if (string.IsNullOrWhiteSpace(command.EntityId))
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.Required(nameof(command.EntityId)));
        }

        if (!TryParsePatch(command.Json, out LocalizedContentPatch? patch))
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(LocalizedContentApplicationErrors.InvalidJson());
        }

        ApplicationResult<LocalizedContentApplyResult> result = entityType switch
        {
            LocalizedContentEntityType.Park => await this.ApplyToParkAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.ParkZone => await this.ApplyToParkZoneAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.ParkItem => await this.ApplyToParkItemAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.ParkOperator => await this.ApplyToParkOperatorAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.ParkFounder => await this.ApplyToParkFounderAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.AttractionManufacturer => await this.ApplyToAttractionManufacturerAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.Image => await this.ApplyToImageAsync(command.EntityId.Trim(), patch, cancellationToken),
            LocalizedContentEntityType.ImageTag => await this.ApplyToImageTagAsync(command.EntityId.Trim(), patch, cancellationToken),
            _ => ApplicationResult<LocalizedContentApplyResult>.Failure(LocalizedContentApplicationErrors.InvalidEntityType(command.EntityType)),
        };

        return result;
    }

    private async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToParkAsync(string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return UnsupportedField(LocalizedContentEntityType.Park, "accessConditions");
        }

        Park? park = await this.parkRepository.GetByIdAsync(entityId, true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Park), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is not "descriptions" and not "description")
            {
                return UnsupportedField(LocalizedContentEntityType.Park, field.Key);
            }

            park.Descriptions = Merge(park.Descriptions, field.Value, patch.ReplaceExisting);
            updatedFields.Add("descriptions");
            updatedValueCount += field.Value.Count;
        }

        Park? updated = await this.parkRepository.UpdateAsync(entityId, park, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Park), entityId));
        }

        await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, updated.Id, cancellationToken);
        IReadOnlyCollection<ParkItem> parkItems = await this.parkItemRepository.GetByParkIdAsync(updated.Id, true, cancellationToken);
        foreach (ParkItem parkItem in parkItems)
        {
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, parkItem.Id, cancellationToken);
        }

        return Success(LocalizedContentEntityType.Park, entityId, updatedFields, updatedValueCount);
    }

    private async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToParkZoneAsync(string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return UnsupportedField(LocalizedContentEntityType.ParkZone, "accessConditions");
        }

        ParkZone? zone = await this.parkZoneRepository.GetByIdAsync(entityId, cancellationToken);
        if (zone is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkZone), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is "names" or "name")
            {
                zone.Names = Merge(zone.Names, field.Value, patch.ReplaceExisting);
                updatedFields.Add("names");
            }
            else if (normalizedField is "descriptions" or "description")
            {
                zone.Descriptions = Merge(zone.Descriptions, field.Value, patch.ReplaceExisting);
                updatedFields.Add("descriptions");
            }
            else
            {
                return UnsupportedField(LocalizedContentEntityType.ParkZone, field.Key);
            }

            updatedValueCount += field.Value.Count;
        }

        ParkZone? updated = await this.parkZoneRepository.UpdateAsync(entityId, zone, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkZone), entityId));
        }

        await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, updated.ParkId, cancellationToken);
        return Success(LocalizedContentEntityType.ParkZone, entityId, updatedFields, updatedValueCount);
    }

    private async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToParkItemAsync(string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        ParkItem? item = await this.parkItemRepository.GetByIdAsync(entityId, true, cancellationToken);
        if (item is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkItem), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is not "descriptions" and not "description")
            {
                return UnsupportedField(LocalizedContentEntityType.ParkItem, field.Key);
            }

            item.Descriptions = Merge(item.Descriptions, field.Value, patch.ReplaceExisting);
            updatedFields.Add("descriptions");
            updatedValueCount += field.Value.Count;
        }

        foreach (AccessConditionPatch accessConditionPatch in patch.AccessConditions)
        {
            ApplicationResult<int> accessResult = await this.ApplyAccessConditionPatchAsync(item, accessConditionPatch, patch.ReplaceExisting, updatedFields, updatedValueCount, cancellationToken);
            if (!accessResult.IsSuccess)
            {
                return ApplicationResult<LocalizedContentApplyResult>.Failure(accessResult.Errors);
            }

            updatedValueCount = accessResult.Value;
        }

        ParkItem? updated = await this.parkItemRepository.UpdateAsync(entityId, item, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkItem), entityId));
        }

        await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, updated.Id, cancellationToken);
        await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, updated.ParkId, cancellationToken);
        return Success(LocalizedContentEntityType.ParkItem, entityId, updatedFields, updatedValueCount);
    }

    private async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToParkOperatorAsync(string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return UnsupportedField(LocalizedContentEntityType.ParkOperator, "accessConditions");
        }

        ParkOperator? entity = await this.parkOperatorRepository.GetByIdAsync(entityId, cancellationToken);
        if (entity is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkOperator), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is not "description" and not "descriptions")
            {
                return UnsupportedField(LocalizedContentEntityType.ParkOperator, field.Key);
            }

            entity.Description = Merge(entity.Description, field.Value, patch.ReplaceExisting);
            updatedFields.Add("description");
            updatedValueCount += field.Value.Count;
        }

        ParkOperator? updated = await this.parkOperatorRepository.UpdateAsync(entityId, entity, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkOperator), entityId));
        }

        await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Operators, updated.Id, cancellationToken);
        return Success(LocalizedContentEntityType.ParkOperator, entityId, updatedFields, updatedValueCount);
    }

    private async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToParkFounderAsync(string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return UnsupportedField(LocalizedContentEntityType.ParkFounder, "accessConditions");
        }

        ParkFounder? entity = await this.parkFounderRepository.GetByIdAsync(entityId, cancellationToken);
        if (entity is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkFounder), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is not "biography" and not "bio")
            {
                return UnsupportedField(LocalizedContentEntityType.ParkFounder, field.Key);
            }

            entity.Biography = Merge(entity.Biography, field.Value, patch.ReplaceExisting);
            updatedFields.Add("biography");
            updatedValueCount += field.Value.Count;
        }

        ParkFounder? updated = await this.parkFounderRepository.UpdateAsync(entityId, entity, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkFounder), entityId));
        }

        await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Founders, updated.Id, cancellationToken);
        return Success(LocalizedContentEntityType.ParkFounder, entityId, updatedFields, updatedValueCount);
    }

    private async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToAttractionManufacturerAsync(string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return UnsupportedField(LocalizedContentEntityType.AttractionManufacturer, "accessConditions");
        }

        AttractionManufacturer? entity = await this.attractionManufacturerRepository.GetByIdAsync(entityId, cancellationToken);
        if (entity is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(AttractionManufacturer), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is not "biography" and not "bio")
            {
                return UnsupportedField(LocalizedContentEntityType.AttractionManufacturer, field.Key);
            }

            entity.Biography = Merge(entity.Biography, field.Value, patch.ReplaceExisting);
            updatedFields.Add("biography");
            updatedValueCount += field.Value.Count;
        }

        AttractionManufacturer? updated = await this.attractionManufacturerRepository.UpdateAsync(entityId, entity, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(AttractionManufacturer), entityId));
        }

        await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, updated.Id, cancellationToken);
        return Success(LocalizedContentEntityType.AttractionManufacturer, entityId, updatedFields, updatedValueCount);
    }

    private async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToImageAsync(string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return UnsupportedField(LocalizedContentEntityType.Image, "accessConditions");
        }

        Image? image = await this.imageRepository.GetByIdAsync(entityId, cancellationToken);
        if (image is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Image), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        List<LocalizedText> altTexts = image.AltTexts;
        List<LocalizedText> captions = image.Captions;
        List<LocalizedText> credits = image.Credits;

        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is "alttexts" or "alt" or "alternativetexts")
            {
                altTexts = Merge(altTexts, field.Value, patch.ReplaceExisting);
                updatedFields.Add("altTexts");
            }
            else if (normalizedField is "captions" or "caption")
            {
                captions = Merge(captions, field.Value, patch.ReplaceExisting);
                updatedFields.Add("captions");
            }
            else if (normalizedField is "credits" or "credit")
            {
                credits = Merge(credits, field.Value, patch.ReplaceExisting);
                updatedFields.Add("credits");
            }
            else
            {
                return UnsupportedField(LocalizedContentEntityType.Image, field.Key);
            }

            updatedValueCount += field.Value.Count;
        }

        ImageMetadataUpdate metadata = new ImageMetadataUpdate
        {
            Description = image.Description,
            GeoLocation = image.GeoLocation is null ? null : new GeoPointValue(image.GeoLocation.Latitude, image.GeoLocation.Longitude),
            AltTexts = ToValues(altTexts),
            Captions = ToValues(captions),
            Credits = ToValues(credits),
            TagIds = image.TagIds,
            Category = image.Category,
            IsPublished = image.IsPublished,
        };

        Image? updated = await this.imageRepository.UpdateMetadataAsync(entityId, metadata, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Image), entityId));
        }

        return Success(LocalizedContentEntityType.Image, entityId, updatedFields, updatedValueCount);
    }

    private async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToImageTagAsync(string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return UnsupportedField(LocalizedContentEntityType.ImageTag, "accessConditions");
        }

        ImageTag? tag = await this.imageTagRepository.GetByIdAsync(entityId, cancellationToken);
        if (tag is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ImageTag), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        List<LocalizedText> labels = tag.Labels;
        List<LocalizedText> descriptions = tag.Descriptions;

        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is "labels" or "label")
            {
                labels = Merge(labels, field.Value, patch.ReplaceExisting);
                updatedFields.Add("labels");
            }
            else if (normalizedField is "descriptions" or "description")
            {
                descriptions = Merge(descriptions, field.Value, patch.ReplaceExisting);
                updatedFields.Add("descriptions");
            }
            else
            {
                return UnsupportedField(LocalizedContentEntityType.ImageTag, field.Key);
            }

            updatedValueCount += field.Value.Count;
        }

        ImageTagWriteModel model = new ImageTagWriteModel
        {
            Slug = tag.Slug,
            Labels = ToValues(labels),
            Descriptions = ToValues(descriptions),
            IsActive = tag.IsActive,
        };

        ImageTag? updated = await this.imageTagRepository.UpdateAsync(entityId, model, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ImageTag), entityId));
        }

        return Success(LocalizedContentEntityType.ImageTag, entityId, updatedFields, updatedValueCount);
    }

    private async Task<ApplicationResult<int>> ApplyAccessConditionPatchAsync(
        ParkItem item,
        AccessConditionPatch patch,
        bool replaceExisting,
        List<string> updatedFields,
        int updatedValueCount,
        CancellationToken cancellationToken)
    {
        if (item.Category != ParkItemCategory.Attraction)
        {
            return ApplicationResult<int>.Failure(LocalizedContentApplicationErrors.AccessConditionsRequireAttraction());
        }

        AttractionAccessConditionTypeDefinition? typeDefinition = await this.ResolveAccessConditionTypeDefinitionAsync(patch, cancellationToken);
        string? resolvedTypeKey = typeDefinition?.Key ?? patch.TypeKey;
        AttractionAccessConditionType resolvedLegacyType = typeDefinition?.LegacyType ?? patch.Type;

        item.AttractionDetails ??= new AttractionDetails();
        List<AttractionAccessCondition> conditions = item.AttractionDetails.AccessConditions;
        IReadOnlyCollection<AttractionAccessCondition> matches = FindMatchingAccessConditions(conditions, patch, resolvedTypeKey, resolvedLegacyType);

        if (matches.Count > 1)
        {
            return ApplicationResult<int>.Failure(LocalizedContentApplicationErrors.AccessConditionAmbiguous(patch.Selector));
        }

        AttractionAccessCondition condition;
        if (matches.Count == 1)
        {
            condition = matches.Single();
        }
        else if (patch.CanCreate)
        {
            condition = CreateAccessCondition(conditions, patch, resolvedTypeKey, resolvedLegacyType);
            conditions.Add(condition);
            updatedFields.Add($"accessConditions[{patch.Selector}]");
        }
        else
        {
            return ApplicationResult<int>.Failure(LocalizedContentApplicationErrors.AccessConditionInvalid(patch.Selector));
        }

        ApplyAccessConditionValues(condition, patch, resolvedTypeKey, resolvedLegacyType, replaceExisting, updatedFields, ref updatedValueCount);
        return ApplicationResult<int>.Success(updatedValueCount);
    }

    private async Task<AttractionAccessConditionTypeDefinition?> ResolveAccessConditionTypeDefinitionAsync(AccessConditionPatch patch, CancellationToken cancellationToken)
    {
        string? typeKey = patch.TypeKey;
        if (string.IsNullOrWhiteSpace(typeKey) && patch.HasExplicitType && patch.Type != AttractionAccessConditionType.Custom)
        {
            typeKey = AttractionAccessConditionTypeKeyNormalizer.Normalize(patch.Type.ToString());
        }

        if (string.IsNullOrWhiteSpace(typeKey))
        {
            return null;
        }

        AttractionAccessConditionTypeDefinition? existing = await this.accessConditionTypeDefinitionRepository.GetByKeyAsync(typeKey, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        IReadOnlyCollection<LocalizedTextValue> labels = patch.TypeLabel.Count > 0
            ? ToValues(patch.TypeLabel)
            : AttractionAccessConditionTypeDefaultCatalog.FallbackLabels(typeKey, patch.RawType);

        AttractionAccessConditionTypeDefinitionWriteModel model = new AttractionAccessConditionTypeDefinitionWriteModel
        {
            Key = typeKey,
            LegacyType = patch.Type,
            IsSystem = false,
            IsActive = true,
            SortOrder = 1000,
            Labels = labels,
            Descriptions = Array.Empty<LocalizedTextValue>(),
        };

        return await this.accessConditionTypeDefinitionRepository.UpsertAsync(model, cancellationToken);
    }

    private static IReadOnlyCollection<AttractionAccessCondition> FindMatchingAccessConditions(
        IEnumerable<AttractionAccessCondition> conditions,
        AccessConditionPatch patch,
        string? resolvedTypeKey,
        AttractionAccessConditionType resolvedLegacyType)
    {
        if (!string.IsNullOrWhiteSpace(resolvedTypeKey))
        {
            return conditions
                .Where(condition => string.Equals(AttractionAccessConditionTypeKeyNormalizer.Normalize(condition.TypeKey), resolvedTypeKey, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(AttractionAccessConditionTypeKeyNormalizer.Normalize(condition.CustomTypeKey), resolvedTypeKey, StringComparison.OrdinalIgnoreCase) ||
                                    (string.IsNullOrWhiteSpace(condition.TypeKey) && condition.Type == resolvedLegacyType))
                .Where(condition => !patch.DisplayOrder.HasValue || condition.DisplayOrder == patch.DisplayOrder.Value)
                .ToList();
        }

        if (patch.DisplayOrder.HasValue)
        {
            return conditions
                .Where(condition => condition.DisplayOrder == patch.DisplayOrder.Value)
                .Where(condition => !patch.HasExplicitType || condition.Type == resolvedLegacyType)
                .ToList();
        }

        if (patch.HasExplicitType)
        {
            return conditions.Where(condition => condition.Type == resolvedLegacyType).ToList();
        }

        return Array.Empty<AttractionAccessCondition>();
    }

    private static AttractionAccessCondition CreateAccessCondition(
        IReadOnlyCollection<AttractionAccessCondition> existingConditions,
        AccessConditionPatch patch,
        string? resolvedTypeKey,
        AttractionAccessConditionType resolvedLegacyType)
    {
        return new AttractionAccessCondition
        {
            Type = resolvedLegacyType,
            TypeKey = resolvedTypeKey,
            IsCustom = patch.IsCustom,
            CustomTypeKey = null,
            CustomTypeLabel = new List<LocalizedText>(),
            DisplayOrder = patch.DisplayOrder ?? NextAccessConditionDisplayOrder(existingConditions),
        };
    }

    private static int NextAccessConditionDisplayOrder(IEnumerable<AttractionAccessCondition> existingConditions)
    {
        int maxDisplayOrder = existingConditions
            .Select(condition => condition.DisplayOrder ?? 0)
            .DefaultIfEmpty(0)
            .Max();

        return maxDisplayOrder + 1;
    }

    private static void ApplyAccessConditionValues(
        AttractionAccessCondition condition,
        AccessConditionPatch patch,
        string? resolvedTypeKey,
        AttractionAccessConditionType resolvedLegacyType,
        bool replaceExisting,
        List<string> updatedFields,
        ref int updatedValueCount)
    {
        if (patch.HasExplicitType)
        {
            condition.Type = resolvedLegacyType;
            updatedFields.Add($"accessConditions[{patch.Selector}].type");
        }

        if (!string.IsNullOrWhiteSpace(resolvedTypeKey))
        {
            condition.TypeKey = resolvedTypeKey;
            condition.CustomTypeKey = null;
            condition.CustomTypeLabel = new List<LocalizedText>();
            updatedFields.Add($"accessConditions[{patch.Selector}].typeKey");
        }

        if (patch.IsCustom.HasValue)
        {
            condition.IsCustom = patch.IsCustom.Value ? true : null;
            updatedFields.Add($"accessConditions[{patch.Selector}].isCustom");
        }
        else if (!string.IsNullOrWhiteSpace(condition.TypeKey))
        {
            condition.IsCustom = null;
        }

        if (patch.Value.HasValue)
        {
            condition.Value = patch.Value.Value;
            updatedFields.Add($"accessConditions[{patch.Selector}].value");
        }

        if (patch.Unit.HasValue)
        {
            condition.Unit = patch.Unit.Value;
            updatedFields.Add($"accessConditions[{patch.Selector}].unit");
        }

        if (patch.RequiresAccompaniment.HasValue)
        {
            condition.RequiresAccompaniment = patch.RequiresAccompaniment.Value;
            updatedFields.Add($"accessConditions[{patch.Selector}].requiresAccompaniment");
        }

        if (patch.MinimumCompanionAge.HasValue)
        {
            condition.MinimumCompanionAge = patch.MinimumCompanionAge.Value;
            updatedFields.Add($"accessConditions[{patch.Selector}].minimumCompanionAge");
        }

        if (patch.DisplayOrder.HasValue)
        {
            condition.DisplayOrder = patch.DisplayOrder.Value;
            updatedFields.Add($"accessConditions[{patch.Selector}].displayOrder");
        }

        if (patch.Label.Count > 0)
        {
            condition.Label = Merge(condition.Label, patch.Label, replaceExisting);
            updatedFields.Add($"accessConditions[{patch.Selector}].label");
            updatedValueCount += patch.Label.Count;
        }

        if (patch.Description.Count > 0)
        {
            condition.Description = Merge(condition.Description, patch.Description, replaceExisting);
            updatedFields.Add($"accessConditions[{patch.Selector}].description");
            updatedValueCount += patch.Description.Count;
        }
    }

    private static ApplicationResult<LocalizedContentApplyResult> Success(LocalizedContentEntityType entityType, string entityId, IReadOnlyCollection<string> updatedFields, int updatedValueCount)
    {
        return ApplicationResult<LocalizedContentApplyResult>.Success(new LocalizedContentApplyResult(
            LocalizedContentEntityTypeParser.ToApiValue(entityType),
            entityId,
            updatedFields.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            updatedValueCount));
    }

    private static ApplicationResult<LocalizedContentApplyResult> UnsupportedField(LocalizedContentEntityType entityType, string fieldName)
    {
        return ApplicationResult<LocalizedContentApplyResult>.Failure(LocalizedContentApplicationErrors.UnsupportedField(entityType, fieldName));
    }

    private static List<LocalizedText> Merge(IEnumerable<LocalizedText>? existing, IEnumerable<LocalizedText> incoming, bool replaceExisting)
    {
        Dictionary<string, LocalizedText> values = new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase);
        if (!replaceExisting)
        {
            foreach (LocalizedText value in existing ?? Array.Empty<LocalizedText>())
            {
                string languageCode = NormalizeLanguageCode(value.LanguageCode);
                if (!string.IsNullOrWhiteSpace(languageCode) && !string.IsNullOrWhiteSpace(value.Value))
                {
                    values[languageCode] = new LocalizedText(languageCode, value.Value.Trim());
                }
            }
        }

        foreach (LocalizedText value in incoming)
        {
            string languageCode = NormalizeLanguageCode(value.LanguageCode);
            if (!string.IsNullOrWhiteSpace(languageCode) && !string.IsNullOrWhiteSpace(value.Value))
            {
                values[languageCode] = new LocalizedText(languageCode, value.Value.Trim());
            }
        }

        return values.Values.OrderBy(static value => value.LanguageCode, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IReadOnlyCollection<LocalizedTextValue> ToValues(IEnumerable<LocalizedText> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode) && !string.IsNullOrWhiteSpace(value.Value))
            .Select(static value => new LocalizedTextValue(NormalizeLanguageCode(value.LanguageCode), value.Value!.Trim()))
            .ToList();
    }

    private static bool TryParsePatch(string json, out LocalizedContentPatch? patch)
    {
        patch = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            JsonElement root = document.RootElement;
            bool replaceExisting = TryReadReplaceExisting(root);
            Dictionary<string, IReadOnlyCollection<LocalizedText>> fields = new Dictionary<string, IReadOnlyCollection<LocalizedText>>(StringComparer.OrdinalIgnoreCase);
            List<AccessConditionPatch> accessConditions = new List<AccessConditionPatch>();

            foreach (JsonProperty property in root.EnumerateObject())
            {
                string normalizedPropertyName = NormalizeField(property.Name);
                if (normalizedPropertyName is "mode" or "replace" or "replaceexisting" or "entitytype" or "entityid")
                {
                    continue;
                }

                if (normalizedPropertyName is "fields" && property.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty fieldProperty in property.Value.EnumerateObject())
                    {
                        AddLocalizedField(fields, fieldProperty.Name, fieldProperty.Value);
                    }

                    continue;
                }

                if (normalizedPropertyName is "accessconditions" or "attractionaccessconditions")
                {
                    accessConditions.AddRange(ReadAccessConditionPatches(property.Value));
                    continue;
                }

                AddLocalizedField(fields, property.Name, property.Value);
            }

            patch = new LocalizedContentPatch(fields, accessConditions, replaceExisting);
            return fields.Count > 0 || accessConditions.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void AddLocalizedField(Dictionary<string, IReadOnlyCollection<LocalizedText>> fields, string fieldName, JsonElement value)
    {
        IReadOnlyCollection<LocalizedText> localizedValues = ReadLocalizedTexts(value);
        if (localizedValues.Count > 0)
        {
            fields[fieldName] = localizedValues;
        }
    }

    private static IReadOnlyCollection<LocalizedText> ReadLocalizedTexts(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray()
                .Select(ReadLocalizedText)
                .Where(static item => item is not null)
                .Select(static item => item!)
                .ToList();
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            List<LocalizedText> values = new List<LocalizedText>();
            foreach (JsonProperty property in value.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    string? text = property.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        values.Add(new LocalizedText(NormalizeLanguageCode(property.Name), text.Trim()));
                    }
                }
            }

            return values;
        }

        return Array.Empty<LocalizedText>();
    }

    private static LocalizedText? ReadLocalizedText(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? languageCode = null;
        string? text = null;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            string normalizedName = NormalizeField(property.Name);
            if (normalizedName is "languagecode" or "language" or "lang")
            {
                languageCode = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
            }
            else if (normalizedName is "value" or "text" or "html")
            {
                text = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
            }
        }

        if (string.IsNullOrWhiteSpace(languageCode) || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return new LocalizedText(NormalizeLanguageCode(languageCode), text.Trim());
    }

    private static IReadOnlyCollection<AccessConditionPatch> ReadAccessConditionPatches(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            AccessConditionPatch? singlePatch = ReadAccessConditionPatch(value);
            return singlePatch is null ? Array.Empty<AccessConditionPatch>() : new[] { singlePatch };
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<AccessConditionPatch>();
        }

        List<AccessConditionPatch> patches = new List<AccessConditionPatch>();
        foreach (JsonElement item in value.EnumerateArray())
        {
            AccessConditionPatch? patch = ReadAccessConditionPatch(item);
            if (patch is not null)
            {
                patches.Add(patch);
            }
        }

        return patches;
    }

    private static AccessConditionPatch? ReadAccessConditionPatch(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? rawType = null;
        AttractionAccessConditionType type = AttractionAccessConditionType.Custom;
        bool hasExplicitType = false;
        bool parsedKnownType = false;
        string? typeKey = null;
        IReadOnlyCollection<LocalizedText> typeLabel = Array.Empty<LocalizedText>();
        int? displayOrder = null;
        double? numericValue = null;
        AttractionAccessConditionUnit? unit = null;
        bool? requiresAccompaniment = null;
        int? minimumCompanionAge = null;
        bool? isCustom = null;
        IReadOnlyCollection<LocalizedText> label = Array.Empty<LocalizedText>();
        IReadOnlyCollection<LocalizedText> description = Array.Empty<LocalizedText>();

        foreach (JsonProperty property in item.EnumerateObject())
        {
            string normalizedName = NormalizeField(property.Name);
            if (normalizedName is "type" or "conditiontype")
            {
                rawType = ReadString(property.Value);
                if (!string.IsNullOrWhiteSpace(rawType))
                {
                    hasExplicitType = true;
                    parsedKnownType = Enum.TryParse(rawType, true, out type);
                    if (!parsedKnownType)
                    {
                        type = AttractionAccessConditionType.Custom;
                    }
                }
            }
            else if (normalizedName is "customtypekey" or "customkey" or "typekey" or "key")
            {
                typeKey = AttractionAccessConditionTypeKeyNormalizer.Normalize(ReadString(property.Value));
            }
            else if (normalizedName is "customtypelabel" or "customtypelabels" or "typelabel" or "typelabels")
            {
                typeLabel = ReadLocalizedTexts(property.Value);
            }
            else if (normalizedName is "displayorder" or "order")
            {
                displayOrder = ReadInt32(property.Value);
            }
            else if (normalizedName is "value" or "numericvalue")
            {
                numericValue = ReadDouble(property.Value);
            }
            else if (normalizedName is "unit")
            {
                unit = ReadUnit(property.Value);
            }
            else if (normalizedName is "requiresaccompaniment" or "accompanimentrequired" or "requiresadult" or "adultrequired")
            {
                requiresAccompaniment = ReadBoolean(property.Value);
            }
            else if (normalizedName is "minimumcompanionage" or "mincompanionage" or "companionminage" or "minimumaccompanyingage")
            {
                minimumCompanionAge = ReadInt32(property.Value);
            }
            else if (normalizedName is "iscustom")
            {
                isCustom = ReadBoolean(property.Value);
            }
            else if (normalizedName is "label" or "labels")
            {
                label = ReadLocalizedTexts(property.Value);
            }
            else if (normalizedName is "description" or "descriptions")
            {
                description = ReadLocalizedTexts(property.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(rawType))
        {
            typeKey ??= parsedKnownType
                ? AttractionAccessConditionTypeKeyNormalizer.Normalize(type.ToString())
                : AttractionAccessConditionTypeKeyNormalizer.Normalize(rawType);
        }

        if (!string.IsNullOrWhiteSpace(typeKey))
        {
            hasExplicitType = true;
        }

        bool hasAnyPayload = hasExplicitType ||
                             displayOrder.HasValue ||
                             numericValue.HasValue ||
                             unit.HasValue ||
                             requiresAccompaniment.HasValue ||
                             minimumCompanionAge.HasValue ||
                             isCustom.HasValue ||
                             typeLabel.Count > 0 ||
                             label.Count > 0 ||
                             description.Count > 0;

        if (!hasAnyPayload)
        {
            return null;
        }

        return new AccessConditionPatch(
            type,
            hasExplicitType,
            rawType,
            typeKey,
            typeLabel,
            displayOrder,
            numericValue,
            unit,
            requiresAccompaniment,
            minimumCompanionAge,
            isCustom,
            label,
            description);
    }

    private static string? ReadString(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static int? ReadInt32(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int parsed))
        {
            return parsed;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out parsed))
        {
            return parsed;
        }

        return null;
    }

    private static double? ReadDouble(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double parsed))
        {
            return parsed;
        }

        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool? ReadBoolean(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (value.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool parsed))
        {
            return parsed;
        }

        return null;
    }

    private static AttractionAccessConditionUnit? ReadUnit(JsonElement value)
    {
        string? unitValue = ReadString(value);
        return !string.IsNullOrWhiteSpace(unitValue) && Enum.TryParse(unitValue, true, out AttractionAccessConditionUnit parsed)
            ? parsed
            : null;
    }

    private static bool TryReadReplaceExisting(JsonElement root)
    {
        if (root.TryGetProperty("replaceExisting", out JsonElement replaceExisting) && replaceExisting.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return replaceExisting.GetBoolean();
        }

        if (root.TryGetProperty("replace", out JsonElement replace) && replace.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return replace.GetBoolean();
        }

        if (root.TryGetProperty("mode", out JsonElement mode) && mode.ValueKind == JsonValueKind.String)
        {
            return string.Equals(mode.GetString(), "replace", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string NormalizeField(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }

    private static string? NormalizeCustomTypeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        List<char> chars = new List<char>();
        bool previousWasSeparator = false;
        foreach (char character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                chars.Add(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                chars.Add('-');
                previousWasSeparator = true;
            }
        }

        string key = new string(chars.ToArray()).Trim('-');
        return string.IsNullOrWhiteSpace(key) ? null : key;
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode) ? string.Empty : languageCode.Trim().ToLowerInvariant();
    }

    private sealed record LocalizedContentPatch(
        IReadOnlyDictionary<string, IReadOnlyCollection<LocalizedText>> Fields,
        IReadOnlyCollection<AccessConditionPatch> AccessConditions,
        bool ReplaceExisting);

    private sealed record AccessConditionPatch(
        AttractionAccessConditionType Type,
        bool HasExplicitType,
        string? RawType,
        string? TypeKey,
        IReadOnlyCollection<LocalizedText> TypeLabel,
        int? DisplayOrder,
        double? Value,
        AttractionAccessConditionUnit? Unit,
        bool? RequiresAccompaniment,
        int? MinimumCompanionAge,
        bool? IsCustom,
        IReadOnlyCollection<LocalizedText> Label,
        IReadOnlyCollection<LocalizedText> Description)
    {
        public bool CanCreate => HasExplicitType || !string.IsNullOrWhiteSpace(TypeKey);

        public string Selector
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(TypeKey))
                {
                    return $"typeKey:{TypeKey}";
                }

                if (HasExplicitType)
                {
                    return Type.ToString();
                }

                return DisplayOrder.HasValue ? $"displayOrder:{DisplayOrder}" : "unspecified";
            }
        }
    }
}
