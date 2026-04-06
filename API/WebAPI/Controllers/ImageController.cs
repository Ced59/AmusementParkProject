using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Common.General.Localization;
using Dtos.Images;
using Dtos.Images.Creating;
using Dtos.Images.Links;
using Dtos.Shared;
using Entities.Model.Errors;
using Entities.Model.Images;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    public class ImageController : ControllerBase
    {
        private readonly ISavingImageService savingImageService;
        private readonly ILogger<ImageController> logger;
        private readonly IImageStorageService imageStorageService;
        private readonly IImagesQueryHandler imagesQueryHandler;
        private readonly IImageTagsQueryHandler imageTagsQueryHandler;
        private readonly IImageLinksService imageLinksService;

        public ImageController(
            ISavingImageService savingImageService,
            ILogger<ImageController> logger,
            IImageStorageService imageStorageService,
            IImagesQueryHandler imagesQueryHandler,
            IImageTagsQueryHandler imageTagsQueryHandler,
            IImageLinksService imageLinksService)
        {
            this.savingImageService = savingImageService;
            this.logger = logger;
            this.imageStorageService = imageStorageService;
            this.imagesQueryHandler = imagesQueryHandler;
            this.imageTagsQueryHandler = imageTagsQueryHandler;
            this.imageLinksService = imageLinksService;
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

        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetAllAsync()
        {
            IReadOnlyList<Image> images = await imagesQueryHandler.GetAllImagesAsync();
            return Ok(images.Select(MapImage));
        }

        [HttpGet("tags")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<IEnumerable<ImageTagDto>>> GetTagsAsync()
        {
            IReadOnlyList<ImageTag> tags = await imageTagsQueryHandler.GetAllAsync();
            return Ok(tags.Select(MapTag));
        }

        [HttpPost("tags")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ImageTagDto>> CreateTagAsync([FromBody] CreateImageTagRequest request)
        {
            string slug = request.Slug.Trim().ToLowerInvariant();
            ImageTag? existing = await imageTagsQueryHandler.GetBySlugAsync(slug);
            if (existing != null)
            {
                return Conflict();
            }

            ImageTag tag = new()
            {
                Slug = slug,
                Labels = request.Labels.Select(MapLocalized).ToList(),
                Descriptions = request.Descriptions.Select(MapLocalized).ToList(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            ImageTag? created = await imageTagsQueryHandler.CreateAsync(tag);
            return Ok(MapTag(created!));
        }

        [HttpPut("tags/{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ImageTagDto>> UpdateTagAsync(string id, [FromBody] UpdateImageTagRequest request)
        {
            ImageTag? tag = await imageTagsQueryHandler.GetByIdAsync(id);
            if (tag == null)
            {
                return NotFound();
            }

            tag.Slug = request.Slug.Trim().ToLowerInvariant();
            tag.Labels = request.Labels.Select(MapLocalized).ToList();
            tag.Descriptions = request.Descriptions.Select(MapLocalized).ToList();
            tag.IsActive = request.IsActive;
            tag.UpdatedAt = DateTime.UtcNow;

            ImageTag? updated = await imageTagsQueryHandler.UpdateAsync(tag);
            return Ok(MapTag(updated!));
        }

        [HttpGet("{imageId}/metadata")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ImageDto>> GetMetadataAsync([FromRoute] string imageId)
        {
            Image? image = await imagesQueryHandler.GetImageByIdAsync(imageId);
            if (image == null)
            {
                return NotFound();
            }

            return Ok(MapImage(image));
        }

        [HttpPut("{imageId}/metadata")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ImageDto>> UpdateMetadataAsync([FromRoute] string imageId, [FromBody] UpdateImageAssetRequest request)
        {
            Image? image = await imagesQueryHandler.GetImageByIdAsync(imageId);
            if (image == null)
            {
                return NotFound();
            }

            image.Description = request.Description;
            image.GeoLocation = request.GeoLocation == null
                ? null
                : new ImageGeoLocation
                {
                    Latitude = request.GeoLocation.Latitude,
                    Longitude = request.GeoLocation.Longitude
                };
            image.AltTexts = request.AltTexts.Select(MapLocalized).ToList();
            image.Captions = request.Captions.Select(MapLocalized).ToList();
            image.Credits = request.Credits.Select(MapLocalized).ToList();
            image.TagIds = request.TagIds.Distinct().ToList();
            image.IsPublished = request.IsPublished;
            image.UpdatedAt = DateTime.UtcNow;

            Image? updated = await imagesQueryHandler.UpdateImageAsync(image);
            if (updated == null)
            {
                return NotFound();
            }

            return Ok(MapImage(updated));
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

        private static LocalizedItem<string> MapLocalized(LocalizedItemDto<string> item)
        {
            return new LocalizedItem<string>
            {
                LanguageCode = item.LanguageCode,
                Value = item.Value
            };
        }

        private static ImageTagDto MapTag(ImageTag tag)
        {
            return new ImageTagDto
            {
                Id = tag.Id,
                Slug = tag.Slug,
                Labels = tag.Labels.Select(x => new LocalizedItemDto<string> { LanguageCode = x.LanguageCode, Value = x.Value }).ToList(),
                Descriptions = tag.Descriptions.Select(x => new LocalizedItemDto<string> { LanguageCode = x.LanguageCode, Value = x.Value }).ToList(),
                IsActive = tag.IsActive,
                CreatedAt = tag.CreatedAt,
                UpdatedAt = tag.UpdatedAt
            };
        }

        private static ImageDto MapImage(Image image)
        {
            return new ImageDto
            {
                Id = image.Id,
                Category = image.Category switch
                {
                    ImageCategory.AVATAR => ImageCategoryDto.AVATAR,
                    ImageCategory.PARK_LOGO => ImageCategoryDto.PARK_LOGO,
                    ImageCategory.PARK => ImageCategoryDto.PARK,
                    _ => ImageCategoryDto.ATTRACTION
                },
                OwnerType = image.OwnerType switch
                {
                    ImageOwnerType.Park => ImageOwnerTypeDto.PARK,
                    ImageOwnerType.User => ImageOwnerTypeDto.USER,
                    ImageOwnerType.Attraction => ImageOwnerTypeDto.ATTRACTION,
                    _ => ImageOwnerTypeDto.NONE
                },
                OwnerId = image.OwnerId,
                Path = image.Path,
                Description = image.Description,
                IsCurrent = image.IsCurrent,
                IsPublished = image.IsPublished,
                Width = image.Width,
                Height = image.Height,
                SizeInBytes = image.SizeInBytes,
                OriginalFileName = image.OriginalFileName,
                ContentType = image.ContentType,
                GeoLocation = image.GeoLocation == null
                    ? null
                    : new ImageGeoLocationDto
                    {
                        Latitude = image.GeoLocation.Latitude,
                        Longitude = image.GeoLocation.Longitude
                    },
                AltTexts = image.AltTexts.Select(x => new LocalizedItemDto<string> { LanguageCode = x.LanguageCode, Value = x.Value }).ToList(),
                Captions = image.Captions.Select(x => new LocalizedItemDto<string> { LanguageCode = x.LanguageCode, Value = x.Value }).ToList(),
                Credits = image.Credits.Select(x => new LocalizedItemDto<string> { LanguageCode = x.LanguageCode, Value = x.Value }).ToList(),
                TagIds = image.TagIds.ToList(),
                CreatedAt = image.CreatedAt,
                UpdatedAt = image.UpdatedAt
            };
        }
    }
}
