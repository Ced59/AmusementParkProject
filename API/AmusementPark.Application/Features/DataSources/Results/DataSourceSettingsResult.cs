namespace AmusementPark.Application.Features.DataSources.Results;

/// <summary>
/// Paramètres modifiables d'une source externe.
/// </summary>
public sealed class DataSourceSettingsResult
{
    public string SourceKey { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }

    public IReadOnlyDictionary<string, string?> Options { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
