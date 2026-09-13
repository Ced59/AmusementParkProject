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
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;
using AmusementPark.Application.Common.Measurements;

namespace AmusementPark.Application.Features.LocalizedContent.Handlers;
internal static class ApplyLocalizedContentJsonCommandHandlerEntitiesExtensions
{
    internal static async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToParkAsync(this ApplyLocalizedContentJsonCommandHandler processorContext, string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.Park, "accessConditions");
        }

        Park? park = await processorContext.parkRepository.GetByIdAsync(entityId, true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Park), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is not "descriptions" and not "description")
            {
                return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.Park, field.Key);
            }

            park.Descriptions = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(park.Descriptions, field.Value, patch.ReplaceExisting);
            updatedFields.Add("descriptions");
            updatedValueCount += field.Value.Count;
        }

        ApplicationResult rawResult = ApplyLocalizedContentJsonCommandHandlerRawFieldsExtensions.ApplyParkRawFields(park, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
        }

        Park? updated = await processorContext.parkRepository.UpdateAsync(entityId, park, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Park), entityId));
        }

        await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, updated.Id, cancellationToken);
        IReadOnlyCollection<ParkItem> parkItems = await processorContext.parkItemRepository.GetByParkIdAsync(updated.Id, true, cancellationToken);
        foreach (ParkItem parkItem in parkItems)
        {
            await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, parkItem.Id, cancellationToken);
        }

        return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Success(LocalizedContentEntityType.Park, entityId, updatedFields, updatedValueCount);
    }

    internal static async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToParkZoneAsync(this ApplyLocalizedContentJsonCommandHandler processorContext, string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.ParkZone, "accessConditions");
        }

        ParkZone? zone = await processorContext.parkZoneRepository.GetByIdAsync(entityId, cancellationToken);
        if (zone is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkZone), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is "names" or "name")
            {
                zone.Names = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(zone.Names, field.Value, patch.ReplaceExisting);
                updatedFields.Add("names");
            }
            else if (normalizedField is "descriptions" or "description")
            {
                zone.Descriptions = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(zone.Descriptions, field.Value, patch.ReplaceExisting);
                updatedFields.Add("descriptions");
            }
            else
            {
                return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.ParkZone, field.Key);
            }

            updatedValueCount += field.Value.Count;
        }

        ApplicationResult rawResult = ApplyLocalizedContentJsonCommandHandlerRawFieldsExtensions.ApplyParkZoneRawFields(zone, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
        }

        ParkZone? updated = await processorContext.parkZoneRepository.UpdateAsync(entityId, zone, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkZone), entityId));
        }

        await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, updated.ParkId, cancellationToken);
        return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Success(LocalizedContentEntityType.ParkZone, entityId, updatedFields, updatedValueCount);
    }

    internal static async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToParkItemAsync(this ApplyLocalizedContentJsonCommandHandler processorContext, string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        ParkItem? item = await processorContext.parkItemRepository.GetByIdAsync(entityId, true, cancellationToken);
        if (item is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkItem), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is not "descriptions" and not "description")
            {
                return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.ParkItem, field.Key);
            }

            item.Descriptions = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(item.Descriptions, field.Value, patch.ReplaceExisting);
            updatedFields.Add("descriptions");
            updatedValueCount += field.Value.Count;
        }

        ApplicationResult rawResult = ApplyLocalizedContentJsonCommandHandlerRawFieldsExtensions.ApplyParkItemRawFields(item, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
        }

        foreach (AccessConditionPatch accessConditionPatch in patch.AccessConditions)
        {
            ApplicationResult<int> accessResult = await processorContext.ApplyAccessConditionPatchAsync(item, accessConditionPatch, patch.ReplaceExisting, updatedFields, updatedValueCount, cancellationToken);
            if (!accessResult.IsSuccess)
            {
                return ApplicationResult<LocalizedContentApplyResult>.Failure(accessResult.Errors);
            }

            updatedValueCount = accessResult.Value;
        }

        if (ApplyLocalizedContentJsonCommandHandlerAttractionDetailsExtensions.ShouldNormalizeAttractionDetails(patch) && item.AttractionDetails is not null)
        {
            processorContext.NormalizeAttractionDetailsAfterPatch(item.AttractionDetails, updatedFields);
        }

        ParkItem? updated = await processorContext.parkItemRepository.UpdateAsync(entityId, item, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkItem), entityId));
        }

        await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, updated.Id, cancellationToken);
        await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, updated.ParkId, cancellationToken);
        return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Success(LocalizedContentEntityType.ParkItem, entityId, updatedFields, updatedValueCount);
    }

    internal static async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToParkOperatorAsync(this ApplyLocalizedContentJsonCommandHandler processorContext, string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.ParkOperator, "accessConditions");
        }

        ParkOperator? entity = await processorContext.parkOperatorRepository.GetByIdAsync(entityId, cancellationToken);
        if (entity is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkOperator), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is not "description" and not "descriptions")
            {
                return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.ParkOperator, field.Key);
            }

            entity.Description = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(entity.Description, field.Value, patch.ReplaceExisting);
            updatedFields.Add("description");
            updatedValueCount += field.Value.Count;
        }

        ApplicationResult rawResult = ApplyLocalizedContentJsonCommandHandlerRawFieldsExtensions.ApplyParkOperatorRawFields(entity, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
        }

        ParkOperator? updated = await processorContext.parkOperatorRepository.UpdateAsync(entityId, entity, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkOperator), entityId));
        }

        await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Operators, updated.Id, cancellationToken);
        return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Success(LocalizedContentEntityType.ParkOperator, entityId, updatedFields, updatedValueCount);
    }

    internal static async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToParkFounderAsync(this ApplyLocalizedContentJsonCommandHandler processorContext, string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.ParkFounder, "accessConditions");
        }

        ParkFounder? entity = await processorContext.parkFounderRepository.GetByIdAsync(entityId, cancellationToken);
        if (entity is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkFounder), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is not "biography" and not "bio")
            {
                return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.ParkFounder, field.Key);
            }

            entity.Biography = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(entity.Biography, field.Value, patch.ReplaceExisting);
            updatedFields.Add("biography");
            updatedValueCount += field.Value.Count;
        }

        ApplicationResult rawResult = ApplyLocalizedContentJsonCommandHandlerRawFieldsExtensions.ApplyParkFounderRawFields(entity, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
        }

        ParkFounder? updated = await processorContext.parkFounderRepository.UpdateAsync(entityId, entity, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkFounder), entityId));
        }

        await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Founders, updated.Id, cancellationToken);
        return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Success(LocalizedContentEntityType.ParkFounder, entityId, updatedFields, updatedValueCount);
    }

    internal static async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToAttractionManufacturerAsync(this ApplyLocalizedContentJsonCommandHandler processorContext, string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.AttractionManufacturer, "accessConditions");
        }

        AttractionManufacturer? entity = await processorContext.attractionManufacturerRepository.GetByIdAsync(entityId, cancellationToken);
        if (entity is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(AttractionManufacturer), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is not "biography" and not "bio")
            {
                return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.AttractionManufacturer, field.Key);
            }

            entity.Biography = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(entity.Biography, field.Value, patch.ReplaceExisting);
            updatedFields.Add("biography");
            updatedValueCount += field.Value.Count;
        }

        ApplicationResult rawResult = ApplyLocalizedContentJsonCommandHandlerRawFieldsExtensions.ApplyAttractionManufacturerRawFields(entity, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
        }

        AttractionManufacturer? updated = await processorContext.attractionManufacturerRepository.UpdateAsync(entityId, entity, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(AttractionManufacturer), entityId));
        }

        await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, updated.Id, cancellationToken);
        return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Success(LocalizedContentEntityType.AttractionManufacturer, entityId, updatedFields, updatedValueCount);
    }

    internal static async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToAccessConditionTypeAsync(this ApplyLocalizedContentJsonCommandHandler processorContext, string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.AccessConditionType, "accessConditions");
        }

        IReadOnlyCollection<AttractionAccessConditionTypeDefinition> definitions = await processorContext.accessConditionTypeDefinitionRepository.GetAllAsync(true, cancellationToken);
        AttractionAccessConditionTypeDefinition? existing = definitions.FirstOrDefault(value => string.Equals(value.Id, entityId, StringComparison.OrdinalIgnoreCase) || string.Equals(value.Key, entityId, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(AttractionAccessConditionTypeDefinition), entityId));
        }

        List<string> updatedFields = new List<string>();
        int updatedValueCount = 0;
        List<LocalizedText> labels = existing.Labels;
        List<LocalizedText> descriptions = existing.Descriptions;
        string key = existing.Key;
        AttractionAccessConditionType legacyType = existing.LegacyType;
        bool isActive = existing.IsActive;
        int sortOrder = existing.SortOrder;
        foreach (KeyValuePair<string, IReadOnlyCollection<LocalizedText>> field in patch.Fields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is "labels" or "label" or "typelabel" or "typelabels")
            {
                labels = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(labels, field.Value, patch.ReplaceExisting);
                updatedFields.Add("labels");
                updatedValueCount += field.Value.Count;
            }
            else if (normalizedField is "descriptions" or "description")
            {
                descriptions = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(descriptions, field.Value, patch.ReplaceExisting);
                updatedFields.Add("descriptions");
                updatedValueCount += field.Value.Count;
            }
            else
            {
                return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.AccessConditionType, field.Key);
            }
        }

        foreach (KeyValuePair<string, JsonElement> field in patch.RawFields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is "key" or "typekey")
            {
                string? value = AttractionAccessConditionTypeKeyNormalizer.Normalize(ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadString(field.Value));
                if (!string.IsNullOrWhiteSpace(value))
                {
                    key = value;
                    updatedFields.Add("key");
                }
            }
            else if (normalizedField is "legacytype" or "type")
            {
                AttractionAccessConditionType? value = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ReadEnum<AttractionAccessConditionType>(field.Value);
                if (value.HasValue)
                {
                    legacyType = value.Value;
                    updatedFields.Add("legacyType");
                }
            }
            else if (normalizedField is "isactive" or "active")
            {
                bool? value = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadBoolean(field.Value);
                if (value.HasValue)
                {
                    isActive = value.Value;
                    updatedFields.Add("isActive");
                }
            }
            else if (normalizedField is "sortorder" or "displayorder" or "order")
            {
                int? value = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadInt32(field.Value);
                if (value.HasValue)
                {
                    sortOrder = value.Value;
                    updatedFields.Add("sortOrder");
                }
            }
            else
            {
                return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.AccessConditionType, field.Key);
            }
        }

        AttractionAccessConditionTypeDefinitionWriteModel model = new AttractionAccessConditionTypeDefinitionWriteModel
        {
            Key = key,
            LegacyType = legacyType,
            IsSystem = existing.IsSystem,
            IsActive = isActive,
            SortOrder = sortOrder,
            Labels = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ToValues(labels),
            Descriptions = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ToValues(descriptions),
        };
        AttractionAccessConditionTypeDefinition updated = await processorContext.accessConditionTypeDefinitionRepository.UpsertAsync(model, cancellationToken);
        return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Success(LocalizedContentEntityType.AccessConditionType, updated.Id, updatedFields, updatedValueCount);
    }

    internal static async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToImageAsync(this ApplyLocalizedContentJsonCommandHandler processorContext, string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.Image, "accessConditions");
        }

        if (patch.RawFields.Count > 0)
        {
            return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.Image, patch.RawFields.Keys.First());
        }

        Image? image = await processorContext.imageRepository.GetByIdAsync(entityId, cancellationToken);
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
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is "alttexts" or "alt" or "alternativetexts")
            {
                altTexts = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(altTexts, field.Value, patch.ReplaceExisting);
                updatedFields.Add("altTexts");
            }
            else if (normalizedField is "captions" or "caption")
            {
                captions = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(captions, field.Value, patch.ReplaceExisting);
                updatedFields.Add("captions");
            }
            else if (normalizedField is "credits" or "credit")
            {
                credits = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(credits, field.Value, patch.ReplaceExisting);
                updatedFields.Add("credits");
            }
            else
            {
                return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.Image, field.Key);
            }

            updatedValueCount += field.Value.Count;
        }

        ImageMetadataUpdate metadata = new ImageMetadataUpdate
        {
            Description = image.Description,
            GeoLocation = image.GeoLocation is null ? null : new GeoPointValue(image.GeoLocation.Latitude, image.GeoLocation.Longitude),
            AltTexts = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ToValues(altTexts),
            Captions = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ToValues(captions),
            Credits = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ToValues(credits),
            TagIds = image.TagIds,
            Category = image.Category,
            IsPublished = image.IsPublished,
            SourceUrl = image.SourceUrl,
        };
        Image? updated = await processorContext.imageRepository.UpdateMetadataAsync(entityId, metadata, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Image), entityId));
        }

        return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Success(LocalizedContentEntityType.Image, entityId, updatedFields, updatedValueCount);
    }

    internal static async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToImageTagAsync(this ApplyLocalizedContentJsonCommandHandler processorContext, string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.ImageTag, "accessConditions");
        }

        ImageTag? tag = await processorContext.imageTagRepository.GetByIdAsync(entityId, cancellationToken);
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
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is "labels" or "label")
            {
                labels = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(labels, field.Value, patch.ReplaceExisting);
                updatedFields.Add("labels");
            }
            else if (normalizedField is "descriptions" or "description")
            {
                descriptions = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Merge(descriptions, field.Value, patch.ReplaceExisting);
                updatedFields.Add("descriptions");
            }
            else
            {
                return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.UnsupportedField(LocalizedContentEntityType.ImageTag, field.Key);
            }

            updatedValueCount += field.Value.Count;
        }

        ApplicationResult rawResult = ApplyLocalizedContentJsonCommandHandlerRawFieldsExtensions.ApplyImageTagRawFields(tag, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
        }

        ImageTagWriteModel model = new ImageTagWriteModel
        {
            Slug = tag.Slug,
            Labels = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ToValues(labels),
            Descriptions = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ToValues(descriptions),
            IsActive = tag.IsActive,
        };
        ImageTag? updated = await processorContext.imageTagRepository.UpdateAsync(entityId, model, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ImageTag), entityId));
        }

        return ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.Success(LocalizedContentEntityType.ImageTag, entityId, updatedFields, updatedValueCount);
    }
}
