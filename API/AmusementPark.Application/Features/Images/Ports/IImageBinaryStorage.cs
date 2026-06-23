namespace AmusementPark.Application.Features.Images.Ports;

/// <summary>
/// Port de stockage binaire spécialisé pour les images et leurs variantes.
/// </summary>
public interface IImageBinaryStorage
{
    /// <summary>
    /// Sauvegarde les variantes techniques d'une image.
    /// </summary>
    Task<IReadOnlyCollection<string>> SaveAsync(
        string pathWithoutExtension,
        AmusementPark.Application.Common.Contracts.FilePayload file,
        bool withWatermark,
        CancellationToken cancellationToken);

    /// <summary>
    /// Récupère la meilleure variante disponible pour un client HTTP.
    /// </summary>
    Task<(Stream Stream, string ContentType)?> GetBestAsync(
        string pathWithoutExtension,
        string? acceptHeader,
        int? width,
        CancellationToken cancellationToken);

    /// <summary>
    /// RecrÃ©e les variantes d'une image existante avec le watermark applicatif.
    /// </summary>
    Task<bool> ApplyWatermarkAsync(string pathWithoutExtension, CancellationToken cancellationToken);

    /// <summary>
    /// Supprime toutes les variantes connues d'une image.
    /// </summary>
    Task DeleteAsync(string pathWithoutExtension, CancellationToken cancellationToken);
}
