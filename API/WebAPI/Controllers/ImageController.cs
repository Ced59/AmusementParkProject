using Microsoft.AspNetCore.Mvc;
using Services.Interfaces.Images;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("images")]
    public class ImageController : ControllerBase
    {
        private readonly IImageCompressorService compressor;
        private readonly IImageStorageService storage;
        private readonly ILogger<ImageController> logger;

        public ImageController(IImageCompressorService compressor, IImageStorageService storage, ILogger<ImageController> logger)
        {
            this.compressor = compressor;
            this.storage = storage;
            this.logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Error = "Fichier manquant ou vide." });

            try
            {
                var baseName = Path.GetFileNameWithoutExtension(file.FileName);
                await using Stream stream = file.OpenReadStream();

                Dictionary<string, byte[]> images = await compressor.CompressAsync(stream, baseName);
                await storage.StoreAsync(images, "amusement-park-images");

                return Ok(new { Success = true, Files = images.Keys });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur pendant l'upload ou la compression.");
                return StatusCode(500, new { Error = "Erreur lors du traitement de l'image." });
            }
        }
    }
}