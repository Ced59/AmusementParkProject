using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkItems;

namespace AmusementPark.WebAPI.Contracts.AttractionAccessConditionTypes;

public sealed class UpsertAttractionAccessConditionTypeDefinitionDto
{
    public string Key { get; set; } = string.Empty;
    public AttractionAccessConditionTypeDto? LegacyType { get; set; }
    public bool IsActive { get; set; } = true;
    public List<LocalizedTextDto>? Labels { get; set; }
    public List<LocalizedTextDto>? Descriptions { get; set; }
    public int? SortOrder { get; set; }
}
