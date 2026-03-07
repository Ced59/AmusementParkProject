using Dtos.Images;
using Entities.Model.Images;
using Dtos.Images.Links;
using Entities.Model.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using Services.Interfaces.Images;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("images")]
    public class ImageLinksController : ControllerBase
    {
        private readonly IImageLinksService imageLinksService;

        public ImageLinksController(IImageLinksService imageLinksService)
        {
            this.imageLinksService = imageLinksService;
        }

        [HttpPost("links")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> LinkImageAsync([FromBody] LinkImageToOwnerDto request)
        {
            OneOf<ImageDto, ErrorCodes.ErrorDetail> result = await imageLinksService.LinkImageAsync(request);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpGet("{ownerType}/{ownerId}/{category}/current")]
        public async Task<IActionResult> GetCurrentImageAsync(
            [FromRoute] string ownerType,
            [FromRoute] string ownerId,
            [FromRoute] string category)
        {
            if (!Enum.TryParse<ImageOwnerType>(ownerType, true, out ImageOwnerType parsedOwnerType))
            {
                return BadRequest("Invalid ownerType.");
            }

            if (!Enum.TryParse<ImageCategory>(category, true, out ImageCategory parsedCategory))
            {
                return BadRequest("Invalid category.");
            }

            OneOf<ImageDto, ErrorCodes.ErrorDetail> result = await imageLinksService.GetCurrentImageAsync(ownerId, parsedOwnerType, parsedCategory);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpGet("{ownerType}/{ownerId}/{category}")]
        public async Task<IActionResult> GetImagesAsync(
            [FromRoute] string ownerType,
            [FromRoute] string ownerId,
            [FromRoute] string category)
        {
            if (!Enum.TryParse<ImageOwnerType>(ownerType, true, out ImageOwnerType parsedOwnerType))
            {
                return BadRequest("Invalid ownerType.");
            }

            if (!Enum.TryParse<ImageCategory>(category, true, out ImageCategory parsedCategory))
            {
                return BadRequest("Invalid category.");
            }

            OneOf<IEnumerable<ImageDto>, ErrorCodes.ErrorDetail> result = await imageLinksService.GetImagesAsync(ownerId, parsedOwnerType, parsedCategory);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpPut("{imageId}/current")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> SetCurrentImageAsync([FromRoute] string imageId)
        {
            OneOf<ImageDto, ErrorCodes.ErrorDetail> result = await imageLinksService.SetCurrentImageAsync(imageId);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpDelete("{imageId}")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> DeleteImageAsync([FromRoute] string imageId)
        {
            OneOf<bool, ErrorCodes.ErrorDetail> result = await imageLinksService.DeleteImageAsync(imageId);
            return ApiResponseHandler.HandleResponse(result);
        }
    }
}