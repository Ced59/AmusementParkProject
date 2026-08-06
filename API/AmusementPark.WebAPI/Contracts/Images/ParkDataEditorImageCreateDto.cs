using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Contracts.Images;

/// <summary>
/// Upload technique Codex. Le fichier emprunte le pipeline d'image commun et le
/// watermark reste désactivé par défaut pour les images récupérées depuis une source.
/// </summary>
public sealed class ParkDataEditorImageCreateDto
{
    public ImageCategoryDto Category { get; set; }

    public IFormFile? File { get; set; }

    public string? Description { get; set; }

    public bool WithWatermark { get; set; }

    public bool IsPublished { get; set; } = true;
}
