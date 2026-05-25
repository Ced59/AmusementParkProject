using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkItems;

namespace AmusementPark.WebAPI.Contracts.AttractionAccessConditionTypes;

public sealed class AttractionAccessConditionTypeDefinitionDto
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public AttractionAccessConditionTypeDto LegacyType { get; set; } = AttractionAccessConditionTypeDto.Custom;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public List<LocalizedTextDto> Labels { get; set; } = new();
    public List<LocalizedTextDto> Descriptions { get; set; } = new();
    public int SortOrder { get; set; }
}
