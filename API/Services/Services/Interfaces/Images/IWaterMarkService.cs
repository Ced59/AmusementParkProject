using System.IO;
using System.Threading.Tasks;

namespace Services.Interfaces.Images
{
    public interface IWaterMarkService
    {
        Task<Stream> ApplyWatermarkAsync(Stream imageStream, string watermarkText);
    }
}