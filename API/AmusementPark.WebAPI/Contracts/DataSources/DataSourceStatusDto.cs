using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class DataSourceStatusDto
{
    public string SourceKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public DateTime? LastSuccessfulImportUtc { get; set; }

    public int TotalSessionsCount { get; set; }
}
