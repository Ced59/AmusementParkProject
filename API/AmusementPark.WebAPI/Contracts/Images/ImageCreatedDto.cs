using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Images;

/// <summary>
/// Contrat HTTP de retour d'upload d'image.
/// </summary>
public sealed class ImageCreatedDto
{
    public string? Id { get; set; }

    public IEnumerable<string>? SavedListFile { get; set; }

    public ImageCategoryDto Category { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public long SizeInBytes { get; set; }
}
