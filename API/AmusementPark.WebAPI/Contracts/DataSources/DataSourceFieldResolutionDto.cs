using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class DataSourceFieldResolutionDto
{
    public string Field { get; set; } = string.Empty;

    public string SourceType { get; set; } = "Variant";

    public string? ExternalVariantId { get; set; }
}
