using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Images;

/// <summary>
/// Tag métier d'image.
/// </summary>
public sealed class ImageTag : AuditableEntity
{
    /// <summary>
    /// Slug stable du tag.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Libellés localisés.
    /// </summary>
    public List<LocalizedText> Labels { get; set; } = new();

    /// <summary>
    /// Descriptions localisées.
    /// </summary>
    public List<LocalizedText> Descriptions { get; set; } = new();

    /// <summary>
    /// Indique si le tag est actif.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
