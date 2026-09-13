namespace AmusementPark.Application.Features.DataSources.Results;

/// <summary>
/// Statut synthétique d'une source externe.
/// </summary>
public sealed class DataSourceStatusResult
{
    public string SourceKey { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }

    public DateTime? LastSuccessfulImportUtc { get; init; }

    public int TotalSessionsCount { get; init; }
}
