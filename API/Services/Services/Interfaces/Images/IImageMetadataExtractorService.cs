using System.IO;
using System.Threading.Tasks;
using Services.Models.Images;

namespace Services.Interfaces.Images
{
    public interface IImageMetadataExtractorService
    {
        Task<ExtractedImageMetadata> ExtractMetadataAsync(Stream imageStream);
    }
}
