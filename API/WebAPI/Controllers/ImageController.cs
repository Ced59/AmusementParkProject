using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dtos.Images.Creating;
using Entities.Model.Errors;
using Entities.Model.Images;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces.Images;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("images")]
    public class ImageController : ControllerBase
    {
        private readonly ISavingImageService savingImageService;
        private readonly ILogger<ImageController> logger;
        private readonly IImageStorageService imageStorageService;
        private readonly IImagesQueryHandler imagesQueryHandler;

        public ImageController(
            ISavingImageService savingImageService,
            ILogger<ImageController> logger,
            IImageStorageService imageStorageService,
            IImagesQueryHandler imagesQueryHandler)
        {
            this.savingImageService = savingImageService;
            this.logger = logger;
            this.imageStorageService = imageStorageService;
            this.imagesQueryHandler = imagesQueryHandler;
        }

        [HttpPost]
        [Authorize(Roles = "USER,MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAsync([FromForm] ImageCreateDto image)
        {
            OneOf<ImageCreatedDto, ErrorCodes.ErrorDetail> result =
                await savingImageService.SaveAsync(image);

            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpGet("{imageId}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetImageAsync([FromRoute] string imageId, CancellationToken cancellationToken)
        {
            Image? image = await imagesQueryHandler.GetImageByIdAsync(imageId);

            if (image == null)
            {
                logger.LogWarning("Image entity not found for id {Id}", imageId);
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(image.Path))
            {
                logger.LogWarning("Image {Id} has no Path defined", imageId);
                return NotFound();
            }

            string pathWithoutExtension = image.Path;
            string? acceptHeader = Request.Headers["Accept"].ToString();

            (Stream Stream, string ContentType)? result = await imageStorageService.GetBestImageAsync(
                pathWithoutExtension,
                acceptHeader,
                cancellationToken);

            if (result == null)
            {
                logger.LogWarning("Image not found for imageId {ImageId}", imageId);
                return NotFound();
            }

            (Stream stream, string contentType) = result.Value;
            return File(stream, contentType);
        }
    }
}