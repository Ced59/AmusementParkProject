using Dtos.Images.Creating;
using Entities.Model.Errors;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using Services.Interfaces.Images;
using WebAPI.ResponseHandlers;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("images")]
    public class ImageController : ControllerBase
    {
        private readonly ISavingImageService savingImageService;
        private readonly ILogger<ImageController> logger;

        public ImageController(ISavingImageService savingImageService, ILogger<ImageController> logger)
        {
            this.savingImageService = savingImageService;
            this.logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAsync([FromForm] ImageCreateDto image)
        {
            OneOf<ImageCreatedDto, ErrorCodes.ErrorDetail> result = await savingImageService.SaveAsync(image);

            return ApiResponseHandler.HandleResponse(result);
        }
    }
}