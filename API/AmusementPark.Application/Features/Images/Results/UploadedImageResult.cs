using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Results;

/// <summary>
/// Résultat applicatif d'un upload d'image.
/// </summary>
public sealed class UploadedImageResult
{
    /// <summary>
    /// Image persistée.
    /// </summary>
    public required Image Image { get; init; }

    /// <summary>
    /// Liste des objets techniques sauvegardés.
    /// </summary>
    public IReadOnlyCollection<string> SavedFiles { get; init; } = Array.Empty<string>();
}
