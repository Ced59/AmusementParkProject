namespace AmusementPark.Application.Features.DataSources.Results;

/// <summary>
/// Compteurs fonctionnels d'une session.
/// </summary>
public sealed class DataSourceMetricsResult
{
    public int ItemsFetchedPrimary { get; init; }

    public int ItemsFetchedSecondary { get; init; }

    public int ComparisonResults { get; init; }

    public int AppliedChanges { get; init; }

    public int DuplicateConflicts { get; init; }

    public int DiscoveredItems { get; init; }

    public int ProcessedItems { get; init; }

    public int FailedItems { get; init; }

    public int SkippedItems { get; init; }
}
