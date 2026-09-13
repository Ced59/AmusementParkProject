namespace AmusementPark.Application.Features.DataSources.Contracts;

/// <summary>
/// Requête d'application de changements issus d'une comparaison.
/// </summary>
public sealed class DataSourceApplyRequest
{
    public string? SessionId { get; init; }

    public IReadOnlyCollection<string> ComparisonResultIds { get; init; } = Array.Empty<string>();

    public bool ApplyAll { get; init; }

    public string? EntityTypeFilter { get; init; }

    public string? ChangeTypeFilter { get; init; }

    public IReadOnlyCollection<DataSourceDuplicateResolution> DuplicateResolutions { get; init; } = Array.Empty<DataSourceDuplicateResolution>();
}
