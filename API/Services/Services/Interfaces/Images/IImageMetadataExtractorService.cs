namespace Services.Interfaces.Images
{
    public interface IImageMetadataExtractorService
    {
        Task<(double? latitude, double? longitude)> ExtractGeoCoordinatesAsync(Stream imageStream);
    }
}