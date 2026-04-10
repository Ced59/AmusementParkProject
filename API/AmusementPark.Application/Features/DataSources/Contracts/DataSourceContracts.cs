namespace AmusementPark.Application.Features.DataSources.Contracts;

/// <summary>
/// Descripteur d'un import de source externe.
/// </summary>
public sealed class DataSourceImportDescriptor
{
    public string ImportKind { get; init; } = "json-files";

    public string WorkingDirectoryPath { get; init; } = string.Empty;

    public IReadOnlyCollection<DataSourceInputFileDescriptor> Files { get; init; } = Array.Empty<DataSourceInputFileDescriptor>();
}

/// <summary>
/// Fichier d'entrée préparé pour un import.
/// </summary>
public sealed class DataSourceInputFileDescriptor
{
    public string Key { get; init; } = string.Empty;

    public string OriginalFileName { get; init; } = string.Empty;

    public string StoredFilePath { get; init; } = string.Empty;
}

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

/// <summary>
/// Choix de source par champ lors d'une fusion manuelle.
/// </summary>
public sealed class DataSourceFieldResolution
{
    public string Field { get; init; } = string.Empty;

    public string SourceType { get; init; } = "Variant";

    public string? ExternalVariantId { get; init; }
}
