using System;
using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Images;

public sealed class ImageExifMetadataDto
{
    public string? CameraMaker { get; set; }

    public string? CameraModel { get; set; }

    public DateTime? TakenOnUtc { get; set; }

    public string? Orientation { get; set; }

    public double? FocalLength { get; set; }

    public double? Aperture { get; set; }

    public double? ExposureTime { get; set; }

    public int? Iso { get; set; }
}
