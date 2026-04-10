using Services.Models.Images;

namespace Services.Interfaces.Images
{
    public interface IImageMetadataExtractorService
    {
        Task<ExtractedImageMetadata> ExtractMetadataAsync(Stream imageStream);
    }
}
