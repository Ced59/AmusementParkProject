using System.Globalization;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLaborsImage = SixLabors.ImageSharp.Image;

namespace AmusementPark.Infrastructure.Services.Images;

/// <summary>
/// Pipeline technique d'extraction des métadonnées d'image.
/// </summary>
public sealed class ImageMetadataPipeline : IImageProcessingPipeline
{
    public Task<ImageUploadRequest> ProcessAsync(ImageUploadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(request);
    }

    public async Task<ImageProcessingMetadata?> ExtractMetadataAsync(ImageUploadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Stream imageStream = request.File.Content;
        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        using SixLaborsImage image = await SixLaborsImage.LoadAsync(imageStream, cancellationToken);
        ExifProfile? exif = image.Metadata.ExifProfile;

        Rational[]? latValues = GetExifReferenceValue(exif, ExifTag.GPSLatitude);
        Rational[]? lonValues = GetExifReferenceValue(exif, ExifTag.GPSLongitude);
        string? latRef = GetExifReferenceValue(exif, ExifTag.GPSLatitudeRef);
        string? lonRef = GetExifReferenceValue(exif, ExifTag.GPSLongitudeRef);
        ushort? orientation = GetExifStructValue(exif, ExifTag.Orientation);
        ushort[]? isoValues = GetExifReferenceValue(exif, ExifTag.ISOSpeedRatings);
        double? latitude = ParseGpsCoordinate(latValues, latRef);
        double? longitude = ParseGpsCoordinate(lonValues, lonRef);

        return new ImageProcessingMetadata
        {
            Width = image.Width,
            Height = image.Height,
            SizeInBytes = imageStream.CanSeek ? imageStream.Length : 0,
            GeoLocation = latitude.HasValue && longitude.HasValue ? new GeoPointValue(latitude.Value, longitude.Value) : null,
            ExifMetadata = exif == null ? null : new ImageExifMetadata
            {
                CameraMaker = GetExifReferenceValue(exif, ExifTag.Make),
                CameraModel = GetExifReferenceValue(exif, ExifTag.Model),
                TakenOnUtc = TryParseDate(GetExifReferenceValue(exif, ExifTag.DateTimeOriginal)),
                Orientation = orientation?.ToString(CultureInfo.InvariantCulture),
                FocalLength = ToNullableDouble(GetExifStructValue(exif, ExifTag.FocalLength)),
                Aperture = ToNullableDouble(GetExifStructValue(exif, ExifTag.FNumber)),
                ExposureTime = ToNullableDouble(GetExifStructValue(exif, ExifTag.ExposureTime)),
                Iso = ToNullableInt(isoValues),
                RawGpsLatitude = latValues == null
                    ? null
                    : string.Join(",", latValues.Select(static value => value.ToDouble().ToString(CultureInfo.InvariantCulture))),
                RawGpsLongitude = lonValues == null
                    ? null
                    : string.Join(",", lonValues.Select(static value => value.ToDouble().ToString(CultureInfo.InvariantCulture))),
            },
        };
    }

    private static T? GetExifReferenceValue<T>(ExifProfile? exif, ExifTag<T> tag)
        where T : class
    {
        if (exif is null)
        {
            return null;
        }

        if (exif.TryGetValue(tag, out IExifValue<T>? exifValue))
        {
            return exifValue.Value;
        }

        return null;
    }

    private static T? GetExifStructValue<T>(ExifProfile? exif, ExifTag<T> tag)
        where T : struct
    {
        if (exif is null)
        {
            return null;
        }

        if (exif.TryGetValue(tag, out IExifValue<T>? exifValue))
        {
            return exifValue.Value;
        }

        return null;
    }

    private static DateTime? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] formats =
        {
            "yyyy:MM:dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss",
        };

        if (DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsedExact))
        {
            return parsedExact;
        }

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsed))
        {
            return parsed;
        }

        return null;
    }

    private static double? ToNullableDouble(object? value)
    {
        return value switch
        {
            Rational rational => rational.ToDouble(),
            Rational[] array when array.Length > 0 => array[0].ToDouble(),
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            int intValue => intValue,
            uint uintValue => uintValue,
            ushort ushortValue => ushortValue,
            short shortValue => shortValue,
            _ => null,
        };
    }

    private static int? ToNullableInt(object? value)
    {
        return value switch
        {
            ushort[] array when array.Length > 0 => array[0],
            short[] array when array.Length > 0 => array[0],
            uint[] array when array.Length > 0 => checked((int)array[0]),
            int[] array when array.Length > 0 => array[0],
            ushort ushortValue => ushortValue,
            short shortValue => shortValue,
            int intValue => intValue,
            uint uintValue => checked((int)uintValue),
            byte byteValue => byteValue,
            _ => null,
        };
    }

    private static double? ParseGpsCoordinate(Rational[]? values, string? direction)
    {
        if (values is not { Length: 3 } || string.IsNullOrWhiteSpace(direction))
        {
            return null;
        }

        double result = values[0].ToDouble() +
                        (values[1].ToDouble() / 60.0) +
                        (values[2].ToDouble() / 3600.0);

        if (direction is "S" or "W")
        {
            result *= -1;
        }

        return result;
    }
}
