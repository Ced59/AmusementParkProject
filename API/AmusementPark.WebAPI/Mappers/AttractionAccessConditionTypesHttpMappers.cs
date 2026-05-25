using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.AttractionAccessConditionTypes;
using AmusementPark.WebAPI.Contracts.ParkItems;

namespace AmusementPark.WebAPI.Mappers;

internal static class AttractionAccessConditionTypesHttpMappers
{
    public static AttractionAccessConditionTypeDefinitionDto ToHttp(this AttractionAccessConditionTypeDefinition value)
    {
        return new AttractionAccessConditionTypeDefinitionDto
        {
            Id = value.Id,
            Key = value.Key,
            LegacyType = value.LegacyType.ToHttp(),
            IsSystem = value.IsSystem,
            IsActive = value.IsActive,
            Labels = value.Labels.ToHttp(),
            Descriptions = value.Descriptions.ToHttp(),
            SortOrder = value.SortOrder,
        };
    }

    public static AttractionAccessConditionTypeDefinitionWriteModel ToApplication(this UpsertAttractionAccessConditionTypeDefinitionDto request)
    {
        return new AttractionAccessConditionTypeDefinitionWriteModel
        {
            Key = request.Key,
            LegacyType = request.LegacyType?.ToDomain() ?? AttractionAccessConditionType.Custom,
            IsSystem = false,
            IsActive = request.IsActive,
            Labels = request.Labels.ToApplicationValues(),
            Descriptions = request.Descriptions.ToApplicationValues(),
            SortOrder = request.SortOrder ?? 1000,
        };
    }

    private static IReadOnlyCollection<LocalizedTextValue> ToApplicationValues(this IEnumerable<AmusementPark.WebAPI.Contracts.Common.LocalizedTextDto>? values)
    {
        if (values is null)
        {
            return Array.Empty<LocalizedTextValue>();
        }

        return values
            .Where(static value => value is not null)
            .Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode) && !string.IsNullOrWhiteSpace(value.Value))
            .Select(static value => new LocalizedTextValue(value.LanguageCode!.Trim().ToLowerInvariant(), value.Value!.Trim()))
            .ToList();
    }

    private static AttractionAccessConditionType ToDomain(this AttractionAccessConditionTypeDto value)
    {
        return Enum.TryParse(value.ToString(), out AttractionAccessConditionType parsed)
            ? parsed
            : AttractionAccessConditionType.Custom;
    }

    private static AttractionAccessConditionTypeDto ToHttp(this AttractionAccessConditionType value)
    {
        return Enum.TryParse(value.ToString(), out AttractionAccessConditionTypeDto parsed)
            ? parsed
            : AttractionAccessConditionTypeDto.Custom;
    }
}
