using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Images;

/// <summary>
/// Patch de masse des métadonnées image côté administration.
/// </summary>
public sealed class BulkImageMetadataUpdateDto
{
    public List<string> ImageIds { get; set; } = new();

    public bool? IsPublished { get; set; }

    public ImageCategoryDto? Category { get; set; }

    public List<string> AddTagIds { get; set; } = new();

    public List<string> RemoveTagIds { get; set; } = new();
}
