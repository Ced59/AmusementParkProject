using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
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

        foreach (AccessConditionLocalizationPatch accessConditionPatch in patch.AccessConditions)
        {
            ApplicationResult accessResult = ApplyAccessConditionPatch(item, accessConditionPatch, patch.ReplaceExisting, updatedFields, ref updatedValueCount);
            if (!accessResult.IsSuccess)
            {
                return ApplicationResult<LocalizedContentApplyResult>.Failure(accessResult.Errors);
            }
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

    private static ApplicationResult ApplyAccessConditionPatch(ParkItem item, AccessConditionLocalizationPatch patch, bool replaceExisting, List<string> updatedFields, ref int updatedValueCount)
    {
        if (item.AttractionDetails is null)
        {
            return ApplicationResult.Failure(LocalizedContentApplicationErrors.AccessConditionNotFound(patch.Selector));
        }

        AttractionAccessCondition? condition = item.AttractionDetails.AccessConditions.FirstOrDefault(condition => MatchesCondition(condition, patch));
        if (condition is null)
        {
            return ApplicationResult.Failure(LocalizedContentApplicationErrors.AccessConditionNotFound(patch.Selector));
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

        return ApplicationResult.Success();
    }

    private static bool MatchesCondition(AttractionAccessCondition condition, AccessConditionLocalizationPatch patch)
    {
        if (patch.DisplayOrder.HasValue && condition.DisplayOrder != patch.DisplayOrder.Value)
        {
            return false;
        }

        return !patch.Type.HasValue || condition.Type == patch.Type.Value;
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
            List<AccessConditionLocalizationPatch> accessConditions = new List<AccessConditionLocalizationPatch>();

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

    private static IReadOnlyCollection<AccessConditionLocalizationPatch> ReadAccessConditionPatches(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<AccessConditionLocalizationPatch>();
        }

        List<AccessConditionLocalizationPatch> patches = new List<AccessConditionLocalizationPatch>();
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            AttractionAccessConditionType? type = null;
            int? displayOrder = null;
            IReadOnlyCollection<LocalizedText> label = Array.Empty<LocalizedText>();
            IReadOnlyCollection<LocalizedText> description = Array.Empty<LocalizedText>();

            foreach (JsonProperty property in item.EnumerateObject())
            {
                string normalizedName = NormalizeField(property.Name);
                if (normalizedName is "type" && property.Value.ValueKind == JsonValueKind.String && Enum.TryParse(property.Value.GetString(), true, out AttractionAccessConditionType parsedType))
                {
                    type = parsedType;
                }
                else if (normalizedName is "displayorder" or "order" && property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out int parsedDisplayOrder))
                {
                    displayOrder = parsedDisplayOrder;
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

            if ((type.HasValue || displayOrder.HasValue) && (label.Count > 0 || description.Count > 0))
            {
                patches.Add(new AccessConditionLocalizationPatch(type, displayOrder, label, description));
            }
        }

        return patches;
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

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode) ? string.Empty : languageCode.Trim().ToLowerInvariant();
    }

    private sealed record LocalizedContentPatch(
        IReadOnlyDictionary<string, IReadOnlyCollection<LocalizedText>> Fields,
        IReadOnlyCollection<AccessConditionLocalizationPatch> AccessConditions,
        bool ReplaceExisting);

    private sealed record AccessConditionLocalizationPatch(
        AttractionAccessConditionType? Type,
        int? DisplayOrder,
        IReadOnlyCollection<LocalizedText> Label,
        IReadOnlyCollection<LocalizedText> Description)
    {
        public string Selector => Type?.ToString() ?? $"displayOrder:{DisplayOrder}";
    }
}
