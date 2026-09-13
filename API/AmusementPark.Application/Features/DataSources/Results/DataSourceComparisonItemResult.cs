namespace AmusementPark.Application.Features.DataSources.Results;

/// <summary>
/// Résultat unitaire de comparaison.
/// </summary>
public sealed class DataSourceComparisonItemResult
{
    public string Id { get; init; } = string.Empty;

    public string EntityType { get; init; } = string.Empty;

    public string ChangeType { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string? LocalEntityId { get; init; }

    public string? ExternalEntityId { get; init; }

    public string MatchConfidence { get; init; } = string.Empty;

    public bool IsApplied { get; init; }

    public bool HasExternalDuplicates { get; init; }

    public bool RequiresManualResolution { get; init; }

    public string ResolutionStatus { get; init; } = string.Empty;

    public string? AppliedExternalVariantId { get; init; }

    public IReadOnlyCollection<DataSourceComparisonFieldChangeResult> Changes { get; init; } = Array.Empty<DataSourceComparisonFieldChangeResult>();

    public IReadOnlyCollection<DataSourceComparisonVariantResult> ExternalVariants { get; init; } = Array.Empty<DataSourceComparisonVariantResult>();
}
