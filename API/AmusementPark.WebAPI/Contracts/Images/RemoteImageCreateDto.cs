using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Contracts.Images;

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
