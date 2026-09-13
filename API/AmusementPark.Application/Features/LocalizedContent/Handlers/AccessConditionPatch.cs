using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Measurements;
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

internal sealed record AccessConditionPatch(
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
