namespace AmusementPark.Application.Features.DataSources.Contracts;

/// <summary>
/// Résolution humaine d'un doublon externe.
/// </summary>
public sealed class DataSourceDuplicateResolution
{
    public string ComparisonResultId { get; init; } = string.Empty;

    public string Strategy { get; init; } = "SelectVariant";

    public string? SelectedExternalVariantId { get; init; }

    public IReadOnlyCollection<DataSourceFieldResolution> FieldResolutions { get; init; } = Array.Empty<DataSourceFieldResolution>();
}
