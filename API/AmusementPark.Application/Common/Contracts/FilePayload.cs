namespace AmusementPark.Application.Common.Contracts;

/// <summary>
/// Représente un fichier binaire reçu par un cas d'usage applicatif.
/// </summary>
public sealed class FilePayload
{
    /// <summary>
    /// Nom de fichier d'origine.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Type MIME du contenu.
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// Taille attendue en octets.
    /// </summary>
    public long Length { get; init; }

    /// <summary>
    /// Flux binaire du contenu.
    /// </summary>
    public Stream Content { get; init; } = Stream.Null;
}
