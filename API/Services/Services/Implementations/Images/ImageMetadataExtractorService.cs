using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Services.Interfaces.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace Services.Implementations.Images
{
    public class ImageMetadataExtractorService : IImageMetadataExtractorService
    {
        public async Task<(double? latitude, double? longitude)> ExtractGeoCoordinatesAsync(Stream imageStream)
        {
            imageStream.Position = 0;
            using Image image = await Image.LoadAsync(imageStream);

            ExifProfile? exif = image.Metadata.ExifProfile;
            if (exif == null)
            {
                return (null, null);
            }

            IExifValue? latTag = exif.Values.FirstOrDefault(v => v.Tag == ExifTag.GPSLatitude);
            IExifValue? latRefTag = exif.Values.FirstOrDefault(v => v.Tag == ExifTag.GPSLatitudeRef);
            IExifValue? lonTag = exif.Values.FirstOrDefault(v => v.Tag == ExifTag.GPSLongitude);
            IExifValue? lonRefTag = exif.Values.FirstOrDefault(v => v.Tag == ExifTag.GPSLongitudeRef);

            double? latitude = ParseGpsCoordinate(latTag?.GetValue() as Rational[], latRefTag?.GetValue() as string);
            double? longitude = ParseGpsCoordinate(lonTag?.GetValue() as Rational[], lonRefTag?.GetValue() as string);

            return (latitude, longitude);
        }

        private double? ParseGpsCoordinate(Rational[]? values, string? direction)
        {
            if (values is not { Length: 3 } || string.IsNullOrWhiteSpace(direction))
            {
                return null;
            }

            double degrees = values[0].ToDouble();
            double minutes = values[1].ToDouble();
            double seconds = values[2].ToDouble();

            double result = degrees + (minutes / 60.0) + (seconds / 3600.0);

            if (direction is "S" or "W")
            {
                result *= -1;
            }

            return result;
        }
    }
}