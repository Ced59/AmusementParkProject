using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Contrainte d'accès HTTP d'une attraction.
/// </summary>
public sealed class AttractionAccessConditionDto
{
    public AttractionAccessConditionTypeDto Type { get; set; }

    public string? TypeKey { get; set; }

    public bool? IsCustom { get; set; }

    public string? CustomTypeKey { get; set; }

    public List<LocalizedTextDto>? CustomTypeLabel { get; set; }

    public double? Value { get; set; }

    public AttractionAccessConditionUnitDto? Unit { get; set; }

    public bool? RequiresAccompaniment { get; set; }

    public int? MinimumCompanionAge { get; set; }

    public List<LocalizedTextDto>? Label { get; set; }

    public List<LocalizedTextDto>? Description { get; set; }

    public int? DisplayOrder { get; set; }
}
