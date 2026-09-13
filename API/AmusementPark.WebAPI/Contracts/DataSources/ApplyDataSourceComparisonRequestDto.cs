using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class ApplyDataSourceComparisonRequestDto
{
    public string? SessionId { get; set; }

    public List<string> ComparisonResultIds { get; set; } = new List<string>();

    public bool ApplyAll { get; set; }

    public string? EntityTypeFilter { get; set; }

    public string? ChangeTypeFilter { get; set; }

    public List<DataSourceDuplicateResolutionDto> DuplicateResolutions { get; set; } = new List<DataSourceDuplicateResolutionDto>();
}
