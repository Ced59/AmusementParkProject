using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class UpdateDataSourceSettingsDto
{
    public bool IsEnabled { get; set; }

    public Dictionary<string, string?> Options { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
