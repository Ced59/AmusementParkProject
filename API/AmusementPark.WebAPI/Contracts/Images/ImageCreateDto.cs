namespace AmusementPark.WebAPI.Contracts.Images;

/// <summary>
/// Contrat HTTP d'upload d'image.
/// </summary>
public sealed class ImageCreateDto
{
    public ImageCategoryDto Category { get; set; }

    public IFormFile? File { get; set; }

    public string? Description { get; set; }

    public bool WithWatermark { get; set; } = true;
}
