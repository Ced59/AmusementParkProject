namespace AmusementPark.Application.Features.DataSources.Contracts;

/// <summary>
/// Fichier d'entrée préparé pour un import.
/// </summary>
public sealed class DataSourceInputFileDescriptor
{
    public string Key { get; init; } = string.Empty;

    public string OriginalFileName { get; init; } = string.Empty;

    public string StoredFilePath { get; init; } = string.Empty;
}
