using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class DataSourceDuplicateResolutionDto
{
    public string ComparisonResultId { get; set; } = string.Empty;

    public string Strategy { get; set; } = "SelectVariant";

    public string? SelectedExternalVariantId { get; set; }

    public List<DataSourceFieldResolutionDto> FieldResolutions { get; set; } = new List<DataSourceFieldResolutionDto>();
}
