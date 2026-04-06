using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Images;

/// <summary>
/// Agrégat métier représentant une image.
/// </summary>
public sealed class Image : AuditableEntity
{
    /// <summary>
    /// Catégorie fonctionnelle.
    /// </summary>
    public ImageCategory Category { get; set; }

    /// <summary>
    /// Chemin technique ou objet distant.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Description libre.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Textes alternatifs localisés.
    /// </summary>
    public List<LocalizedText> AltTexts { get; set; } = new();

    /// <summary>
    /// Légendes localisées.
    /// </summary>
    public List<LocalizedText> Captions { get; set; } = new();

    /// <summary>
    /// Crédits localisés.
    /// </summary>
    public List<LocalizedText> Credits { get; set; } = new();

    /// <summary>
    /// Identifiants des tags associés.
    /// </summary>
    public List<string> TagIds { get; set; } = new();

    /// <summary>
    /// Position GPS extraite si disponible.
    /// </summary>
    public GeoPoint? GeoLocation { get; set; }

    /// <summary>
    /// Métadonnées EXIF extraites.
    /// </summary>
    public ImageExifMetadata? ExifMetadata { get; set; }

    /// <summary>
    /// Largeur en pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Hauteur en pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Taille du fichier en octets.
    /// </summary>
    public long SizeInBytes { get; set; }

    /// <summary>
    /// Type de propriétaire.
    /// </summary>
    public ImageOwnerType OwnerType { get; set; } = ImageOwnerType.None;

    /// <summary>
    /// Identifiant du propriétaire éventuel.
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Indique si l'image est l'image courante du propriétaire.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Nom de fichier original éventuel.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Content type MIME.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Indique si l'image est publiée.
    /// </summary>
    public bool IsPublished { get; set; } = true;
}
