using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class DataSourceComparisonPageDto
{
    public IReadOnlyCollection<DataSourceComparisonItemDto> Items { get; set; } = Array.Empty<DataSourceComparisonItemDto>();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int SessionUpdatedCount { get; set; }

    public int SessionMissingCount { get; set; }

    public int SessionDuplicateCount { get; set; }

    public int SessionAppliedCount { get; set; }
}
