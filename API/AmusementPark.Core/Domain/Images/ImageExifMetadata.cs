namespace AmusementPark.Core.Domain.Images;

/// <summary>
/// Métadonnées EXIF métier extraites d'une image.
/// </summary>
public sealed class ImageExifMetadata
{
    public string? CameraMaker { get; set; }

    public string? CameraModel { get; set; }

    public DateTime? TakenOnUtc { get; set; }

    public string? Orientation { get; set; }

    public double? FocalLength { get; set; }

    public double? Aperture { get; set; }

    public double? ExposureTime { get; set; }

    public int? Iso { get; set; }

    public string? RawGpsLatitude { get; set; }

    public string? RawGpsLongitude { get; set; }
}
