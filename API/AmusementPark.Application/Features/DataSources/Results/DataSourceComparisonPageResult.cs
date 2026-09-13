namespace AmusementPark.Application.Features.DataSources.Results;

/// <summary>
/// Page de résultats de comparaison.
/// </summary>
public sealed class DataSourceComparisonPageResult
{
    public IReadOnlyCollection<DataSourceComparisonItemResult> Items { get; init; } = Array.Empty<DataSourceComparisonItemResult>();

    public int TotalCount { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int SessionUpdatedCount { get; init; }

    public int SessionMissingCount { get; init; }

    public int SessionDuplicateCount { get; init; }

    public int SessionAppliedCount { get; init; }
}
