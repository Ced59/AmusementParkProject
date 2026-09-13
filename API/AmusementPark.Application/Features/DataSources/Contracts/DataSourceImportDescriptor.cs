namespace AmusementPark.Application.Features.DataSources.Contracts;

/// <summary>
/// Descripteur générique d'un import de source externe.
/// </summary>
public sealed class DataSourceImportDescriptor
{
    public string ImportKind { get; init; } = "sitemap";

    public string WorkingDirectoryPath { get; init; } = string.Empty;

    public IReadOnlyCollection<DataSourceInputFileDescriptor> Files { get; init; } = Array.Empty<DataSourceInputFileDescriptor>();

    public IReadOnlyCollection<string> Urls { get; init; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, string?> Options { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    public string? ResumeSessionId { get; init; }
}
