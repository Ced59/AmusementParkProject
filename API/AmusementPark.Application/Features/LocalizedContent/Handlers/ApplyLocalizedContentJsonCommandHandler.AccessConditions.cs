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
}
