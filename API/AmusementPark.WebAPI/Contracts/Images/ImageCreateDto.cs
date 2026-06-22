using Microsoft.AspNetCore.Http;

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

public sealed class RemoteImageCreateDto
{
    public string SourceUrl { get; set; } = string.Empty;

    public ImageCategoryDto Category { get; set; }

    public ImageOwnerTypeDto OwnerType { get; set; } = ImageOwnerTypeDto.NONE;

    public string? OwnerId { get; set; }

    public string? Description { get; set; }

    public bool WithWatermark { get; set; }

    public bool SetAsCurrent { get; set; }
}
