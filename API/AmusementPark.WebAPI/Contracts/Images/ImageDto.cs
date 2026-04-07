using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Images;

/// <summary>
/// Contrat HTTP de lecture d'image.
/// </summary>
public sealed class ImageDto
{
    public required string Id { get; set; }

    public required ImageCategoryDto Category { get; set; }

    public required ImageOwnerTypeDto OwnerType { get; set; }

    public string? OwnerId { get; set; }

    public string? Path { get; set; }

    public string? Description { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsPublished { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public long SizeInBytes { get; set; }

    public string? OriginalFileName { get; set; }

    public string? ContentType { get; set; }

    public ImageGeoLocationDto? GeoLocation { get; set; }

    public List<LocalizedTextDto> AltTexts { get; set; } = new();

    public List<LocalizedTextDto> Captions { get; set; } = new();

    public List<LocalizedTextDto> Credits { get; set; } = new();

    public List<string> TagIds { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
