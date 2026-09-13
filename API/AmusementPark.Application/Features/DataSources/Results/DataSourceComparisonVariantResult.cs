namespace AmusementPark.Application.Features.DataSources.Results;

/// <summary>
/// Variation externe candidate.
/// </summary>
public sealed class DataSourceComparisonVariantResult
{
    public string ExternalVariantId { get; init; } = string.Empty;

    public string DisplayLabel { get; init; } = string.Empty;

    public string? CandidateLocalEntityId { get; init; }

    public string? SourceUrl { get; init; }

    public bool IsSuggested { get; init; }

    public IReadOnlyCollection<DataSourceComparisonFieldChangeResult> Changes { get; init; } = Array.Empty<DataSourceComparisonFieldChangeResult>();
}
