namespace AmusementPark.Application.Features.DataSources.Results;

/// <summary>
/// Différence de champ élémentaire.
/// </summary>
public sealed class DataSourceComparisonFieldChangeResult
{
    public string Field { get; init; } = string.Empty;

    public string? LocalValue { get; init; }

    public string? ExternalValue { get; init; }

    public bool IsDifferent { get; init; }
}
