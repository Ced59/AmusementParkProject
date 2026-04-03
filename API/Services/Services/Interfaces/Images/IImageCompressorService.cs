using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Services.Interfaces.Images
{
    public interface IImageCompressorService
    {
        Task<Dictionary<string, byte[]>> CompressAsync(Stream originalImageStream, string baseFileName);
    }
}