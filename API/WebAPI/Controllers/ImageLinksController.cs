using Dtos.Images;
using Dtos.Images.Creating;
using Dtos.Images.Links;
using Entities.Model.Errors;
using Entities.Model.Images;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces.Images;
using WebAPI.Extensions;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("images")]
    public class ImageLinksController : ControllerBase
    {
        private readonly IImageLinksService imageLinksService;
        private readonly IImagesQueryHandler imagesQueryHandler;

        public ImageLinksController(IImageLinksService imageLinksService, IImagesQueryHandler imagesQueryHandler)
        {
            this.imageLinksService = imageLinksService;
            this.imagesQueryHandler = imagesQueryHandler;
        }

        [HttpPost("links")]
        [Authorize(Roles = "USER,MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> LinkImageAsync([FromBody] LinkImageToOwnerDto request)
        {
            if (!CanManageOwnerImages(request.OwnerType, request.OwnerId))
            {
                return Forbid();
            }

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
        [Authorize(Roles = "USER,MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> SetCurrentImageAsync([FromRoute] string imageId)
        {
            Image? image = await imagesQueryHandler.GetImageByIdAsync(imageId);
            if (image == null)
            {
                return NotFound();
            }

            if (!CanManageOwnerImages(image.OwnerType, image.OwnerId))
            {
                return Forbid();
            }

            OneOf<ImageDto, ErrorCodes.ErrorDetail> result = await imageLinksService.SetCurrentImageAsync(imageId);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpDelete("{imageId}")]
        [Authorize(Roles = "USER,MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> DeleteImageAsync([FromRoute] string imageId)
        {
            Image? image = await imagesQueryHandler.GetImageByIdAsync(imageId);
            if (image == null)
            {
                return NotFound();
            }

            if (!CanManageOwnerImages(image.OwnerType, image.OwnerId))
            {
                return Forbid();
            }

            OneOf<bool, ErrorCodes.ErrorDetail> result = await imageLinksService.DeleteImageAsync(imageId);
            return ApiResponseHandler.HandleResponse(result);
        }

        private bool CanManageOwnerImages(ImageOwnerTypeDto ownerType, string? ownerId)
        {
            ImageOwnerType mappedOwnerType = ownerType switch
            {
                ImageOwnerTypeDto.PARK => ImageOwnerType.Park,
                ImageOwnerTypeDto.USER => ImageOwnerType.User,
                ImageOwnerTypeDto.ATTRACTION => ImageOwnerType.Attraction,
                _ => ImageOwnerType.None
            };

            return CanManageOwnerImages(mappedOwnerType, ownerId);
        }

        private bool CanManageOwnerImages(ImageOwnerType ownerType, string? ownerId)
        {
            string? currentUserId = User.GetUserId();
            bool isAdminOrModerator = User.IsInRoles(Common.Users.Role.ADMIN, Common.Users.Role.MODERATOR);

            if (isAdminOrModerator)
            {
                return true;
            }

            return ownerType == ImageOwnerType.User
                   && !string.IsNullOrWhiteSpace(ownerId)
                   && !string.IsNullOrWhiteSpace(currentUserId)
                   && string.Equals(ownerId, currentUserId, StringComparison.Ordinal);
        }
    }
}
