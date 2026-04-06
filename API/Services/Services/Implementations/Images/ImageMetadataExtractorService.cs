using System.Globalization;
using System.Linq;
using Services.Interfaces.Images;
using Services.Models.Images;
using SixLaborsImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using Entities.Model.Images;
using SixLabors.ImageSharp;

namespace Services.Implementations.Images
{
    public class ImageMetadataExtractorService : IImageMetadataExtractorService
    {
        public async Task<ExtractedImageMetadata> ExtractMetadataAsync(Stream imageStream)
        {
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            using SixLaborsImage image = await SixLaborsImage.LoadAsync(imageStream);
            ExifProfile? exif = image.Metadata.ExifProfile;

            Rational[]? latValues = GetExifReferenceValue(exif, ExifTag.GPSLatitude);
            Rational[]? lonValues = GetExifReferenceValue(exif, ExifTag.GPSLongitude);
            string? latRef = GetExifReferenceValue(exif, ExifTag.GPSLatitudeRef);
            string? lonRef = GetExifReferenceValue(exif, ExifTag.GPSLongitudeRef);
            ushort? orientation = GetExifStructValue(exif, ExifTag.Orientation);
            ushort[]? isoValues = GetExifReferenceValue(exif, ExifTag.ISOSpeedRatings);

            return new ExtractedImageMetadata
            {
                Width = image.Width,
                Height = image.Height,
                SizeInBytes = imageStream.CanSeek ? imageStream.Length : 0,
                Latitude = ParseGpsCoordinate(latValues, latRef),
                Longitude = ParseGpsCoordinate(lonValues, lonRef),
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
                        : string.Join(",", latValues.Select(x => x.ToDouble().ToString(CultureInfo.InvariantCulture))),
                    RawGpsLongitude = lonValues == null
                        ? null
                        : string.Join(",", lonValues.Select(x => x.ToDouble().ToString(CultureInfo.InvariantCulture)))
                }
            };
        }

        private static T? GetExifReferenceValue<T>(ExifProfile? exif, ExifTag<T> tag)
            where T : class
        {
            if (exif == null)
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
            if (exif == null)
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

            string[] formats = new[]
            {
                "yyyy:MM:dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss"
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
                Rational[] arr when arr.Length > 0 => arr[0].ToDouble(),
                double d => d,
                float f => f,
                int i => i,
                uint u => u,
                ushort s => s,
                short s => s,
                _ => null
            };
        }

        private static int? ToNullableInt(object? value)
        {
            return value switch
            {
                ushort[] arr when arr.Length > 0 => arr[0],
                short[] arr when arr.Length > 0 => arr[0],
                uint[] arr when arr.Length > 0 => checked((int)arr[0]),
                int[] arr when arr.Length > 0 => arr[0],
                ushort s => s,
                short s => s,
                int i => i,
                uint u => checked((int)u),
                byte b => b,
                _ => null
            };
        }

        private static double? ParseGpsCoordinate(Rational[]? values, string? direction)
        {
            if (values is not { Length: 3 } || string.IsNullOrWhiteSpace(direction))
            {
                return null;
            }

            double result = values[0].ToDouble()
                + (values[1].ToDouble() / 60.0)
                + (values[2].ToDouble() / 3600.0);

            if (direction is "S" or "W")
            {
                result *= -1;
            }

            return result;
        }
    }
}