namespace AmusementPark.Application.Features.DataSources.Contracts;

/// <summary>
/// Choix de source par champ lors d'une fusion manuelle.
/// </summary>
public sealed class DataSourceFieldResolution
{
    public string Field { get; init; } = string.Empty;

    public string SourceType { get; init; } = "Variant";

    public string? ExternalVariantId { get; init; }
}
