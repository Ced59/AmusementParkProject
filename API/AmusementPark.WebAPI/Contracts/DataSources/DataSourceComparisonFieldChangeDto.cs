using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class DataSourceComparisonFieldChangeDto
{
    public string Field { get; set; } = string.Empty;

    public string? LocalValue { get; set; }

    public string? ExternalValue { get; set; }

    public bool IsDifferent { get; set; }
}
