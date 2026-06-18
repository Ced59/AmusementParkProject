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

namespace AmusementPark.Application.Features.LocalizedContent.Handlers;

public sealed partial class ApplyLocalizedContentJsonCommandHandler
{
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

        ApplicationResult rawResult = ApplyParkRawFields(park, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
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

        ApplicationResult rawResult = ApplyParkZoneRawFields(zone, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
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

        ApplicationResult rawResult = ApplyParkItemRawFields(item, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
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

        if (ShouldNormalizeAttractionDetails(patch) && item.AttractionDetails is not null)
        {
            this.NormalizeAttractionDetailsAfterPatch(item.AttractionDetails, updatedFields);
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

        ApplicationResult rawResult = ApplyParkOperatorRawFields(entity, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
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

        ApplicationResult rawResult = ApplyParkFounderRawFields(entity, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
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

        ApplicationResult rawResult = ApplyAttractionManufacturerRawFields(entity, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
        }

        AttractionManufacturer? updated = await this.attractionManufacturerRepository.UpdateAsync(entityId, entity, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(ApplicationErrors.EntityNotFound(nameof(AttractionManufacturer), entityId));
        }

        await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, updated.Id, cancellationToken);
        return Success(LocalizedContentEntityType.AttractionManufacturer, entityId, updatedFields, updatedValueCount);
    }
    private async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToAccessConditionTypeAsync(string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return UnsupportedField(LocalizedContentEntityType.AccessConditionType, "accessConditions");
        }

        IReadOnlyCollection<AttractionAccessConditionTypeDefinition> definitions = await this.accessConditionTypeDefinitionRepository.GetAllAsync(true, cancellationToken);
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
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is "labels" or "label" or "typelabel" or "typelabels")
            {
                labels = Merge(labels, field.Value, patch.ReplaceExisting);
                updatedFields.Add("labels");
                updatedValueCount += field.Value.Count;
            }
            else if (normalizedField is "descriptions" or "description")
            {
                descriptions = Merge(descriptions, field.Value, patch.ReplaceExisting);
                updatedFields.Add("descriptions");
                updatedValueCount += field.Value.Count;
            }
            else
            {
                return UnsupportedField(LocalizedContentEntityType.AccessConditionType, field.Key);
            }
        }

        foreach (KeyValuePair<string, JsonElement> field in patch.RawFields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is "key" or "typekey")
            {
                string? value = AttractionAccessConditionTypeKeyNormalizer.Normalize(ReadString(field.Value));
                if (!string.IsNullOrWhiteSpace(value))
                {
                    key = value;
                    updatedFields.Add("key");
                }
            }
            else if (normalizedField is "legacytype" or "type")
            {
                AttractionAccessConditionType? value = ReadEnum<AttractionAccessConditionType>(field.Value);
                if (value.HasValue)
                {
                    legacyType = value.Value;
                    updatedFields.Add("legacyType");
                }
            }
            else if (normalizedField is "isactive" or "active")
            {
                bool? value = ReadBoolean(field.Value);
                if (value.HasValue)
                {
                    isActive = value.Value;
                    updatedFields.Add("isActive");
                }
            }
            else if (normalizedField is "sortorder" or "displayorder" or "order")
            {
                int? value = ReadInt32(field.Value);
                if (value.HasValue)
                {
                    sortOrder = value.Value;
                    updatedFields.Add("sortOrder");
                }
            }
            else
            {
                return UnsupportedField(LocalizedContentEntityType.AccessConditionType, field.Key);
            }
        }

        AttractionAccessConditionTypeDefinitionWriteModel model = new AttractionAccessConditionTypeDefinitionWriteModel
        {
            Key = key,
            LegacyType = legacyType,
            IsSystem = existing.IsSystem,
            IsActive = isActive,
            SortOrder = sortOrder,
            Labels = ToValues(labels),
            Descriptions = ToValues(descriptions),
        };

        AttractionAccessConditionTypeDefinition updated = await this.accessConditionTypeDefinitionRepository.UpsertAsync(model, cancellationToken);
        return Success(LocalizedContentEntityType.AccessConditionType, updated.Id, updatedFields, updatedValueCount);
    }
    private async Task<ApplicationResult<LocalizedContentApplyResult>> ApplyToImageAsync(string entityId, LocalizedContentPatch patch, CancellationToken cancellationToken)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return UnsupportedField(LocalizedContentEntityType.Image, "accessConditions");
        }

        if (patch.RawFields.Count > 0)
        {
            return UnsupportedField(LocalizedContentEntityType.Image, patch.RawFields.Keys.First());
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

        ApplicationResult rawResult = ApplyImageTagRawFields(tag, patch.RawFields, updatedFields);
        if (!rawResult.IsSuccess)
        {
            return ApplicationResult<LocalizedContentApplyResult>.Failure(rawResult.Errors);
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
}
