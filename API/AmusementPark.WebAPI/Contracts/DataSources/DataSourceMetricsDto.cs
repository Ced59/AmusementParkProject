using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class DataSourceMetricsDto
{
    public int ItemsFetchedPrimary { get; set; }

    public int ItemsFetchedSecondary { get; set; }

    public int ComparisonResults { get; set; }

    public int AppliedChanges { get; set; }

    public int DuplicateConflicts { get; set; }

    public int DiscoveredItems { get; set; }

    public int ProcessedItems { get; set; }

    public int FailedItems { get; set; }

    public int SkippedItems { get; set; }
}
