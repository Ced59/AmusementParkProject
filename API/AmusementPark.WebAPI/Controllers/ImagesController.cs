using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.AdminPublicView;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature Images migrée en phase 9.
/// </summary>
[ApiController]
[Route("images")]
[RequireActivatedUnblockedUser]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[InvalidatesPublicCache(PublicCacheScope.Data)]
public sealed class ImagesController : ControllerBase
{
    private readonly ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>> uploadImageCommandHandler;
    private readonly ICommandHandler<ImportRemoteImageCommand, ApplicationResult<Image>> importRemoteImageCommandHandler;
    private readonly ICommandHandler<LinkImageCommand, ApplicationResult<Image>> linkImageCommandHandler;
    private readonly ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>> setCurrentImageCommandHandler;
    private readonly ICommandHandler<ApplyImageWatermarkCommand, ApplicationResult<Image>> applyImageWatermarkCommandHandler;
    private readonly ICommandHandler<DeleteImageCommand, ApplicationResult> deleteImageCommandHandler;
    private readonly ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>> updateImageMetadataCommandHandler;
    private readonly ICommandHandler<CreateImageTagCommand, ApplicationResult<ImageTag>> createImageTagCommandHandler;
    private readonly ICommandHandler<UpdateImageTagCommand, ApplicationResult<ImageTag>> updateImageTagCommandHandler;
    private readonly ICommandHandler<UpdateImagesBulkMetadataCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateImagesBulkMetadataCommandHandler;
    private readonly IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>> getImageByIdQueryHandler;
    private readonly IQueryHandler<GetAllImagesQuery, ApplicationResult<IReadOnlyCollection<Image>>> getAllImagesQueryHandler;
    private readonly IQueryHandler<GetImagesPageQuery, ApplicationResult<PagedResult<Image>>> getImagesPageQueryHandler;
    private readonly IQueryHandler<GetParkItemImagesByParkQuery, ApplicationResult<PagedResult<ParkItemImageResult>>> getParkItemImagesByParkQueryHandler;
    private readonly IQueryHandler<GetCurrentImageQuery, ApplicationResult<Image>> getCurrentImageQueryHandler;
    private readonly IQueryHandler<ListImageTagsQuery, ApplicationResult<IReadOnlyCollection<ImageTag>>> listImageTagsQueryHandler;
    private readonly IImageBinaryStorage imageBinaryStorage;

    public ImagesController(
        ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>> uploadImageCommandHandler,
        ICommandHandler<ImportRemoteImageCommand, ApplicationResult<Image>> importRemoteImageCommandHandler,
        ICommandHandler<LinkImageCommand, ApplicationResult<Image>> linkImageCommandHandler,
        ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>> setCurrentImageCommandHandler,
        ICommandHandler<ApplyImageWatermarkCommand, ApplicationResult<Image>> applyImageWatermarkCommandHandler,
        ICommandHandler<DeleteImageCommand, ApplicationResult> deleteImageCommandHandler,
        ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>> updateImageMetadataCommandHandler,
        ICommandHandler<CreateImageTagCommand, ApplicationResult<ImageTag>> createImageTagCommandHandler,
        ICommandHandler<UpdateImageTagCommand, ApplicationResult<ImageTag>> updateImageTagCommandHandler,
        ICommandHandler<UpdateImagesBulkMetadataCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateImagesBulkMetadataCommandHandler,
        IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>> getImageByIdQueryHandler,
        IQueryHandler<GetAllImagesQuery, ApplicationResult<IReadOnlyCollection<Image>>> getAllImagesQueryHandler,
        IQueryHandler<GetImagesPageQuery, ApplicationResult<PagedResult<Image>>> getImagesPageQueryHandler,
        IQueryHandler<GetParkItemImagesByParkQuery, ApplicationResult<PagedResult<ParkItemImageResult>>> getParkItemImagesByParkQueryHandler,
        IQueryHandler<GetCurrentImageQuery, ApplicationResult<Image>> getCurrentImageQueryHandler,
        IQueryHandler<ListImageTagsQuery, ApplicationResult<IReadOnlyCollection<ImageTag>>> listImageTagsQueryHandler,
        IImageBinaryStorage imageBinaryStorage)
    {
        this.uploadImageCommandHandler = uploadImageCommandHandler;
        this.importRemoteImageCommandHandler = importRemoteImageCommandHandler;
        this.linkImageCommandHandler = linkImageCommandHandler;
        this.setCurrentImageCommandHandler = setCurrentImageCommandHandler;
        this.applyImageWatermarkCommandHandler = applyImageWatermarkCommandHandler;
        this.deleteImageCommandHandler = deleteImageCommandHandler;
        this.updateImageMetadataCommandHandler = updateImageMetadataCommandHandler;
        this.createImageTagCommandHandler = createImageTagCommandHandler;
        this.updateImageTagCommandHandler = updateImageTagCommandHandler;
        this.updateImagesBulkMetadataCommandHandler = updateImagesBulkMetadataCommandHandler;
        this.getImageByIdQueryHandler = getImageByIdQueryHandler;
        this.getAllImagesQueryHandler = getAllImagesQueryHandler;
        this.getImagesPageQueryHandler = getImagesPageQueryHandler;
        this.getParkItemImagesByParkQueryHandler = getParkItemImagesByParkQueryHandler;
        this.getCurrentImageQueryHandler = getCurrentImageQueryHandler;
        this.listImageTagsQueryHandler = listImageTagsQueryHandler;
        this.imageBinaryStorage = imageBinaryStorage;
    }

    [HttpPost]
    [AdminAudit("image.upload", "Image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageCreatedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadAsync([FromForm] ImageCreateDto image, CancellationToken cancellationToken = default)
    {
        if (image.File is null)
        {
            return this.ToProblemDetailsResult(StatusCodes.Status400BadRequest, "No image filename provided.", "image.file-required");
        }

        await using System.IO.Stream content = image.File.OpenReadStream();
        FilePayload file = new FilePayload
        {
            FileName = image.File.FileName,
            ContentType = image.File.ContentType,
            Length = image.File.Length,
            Content = content,
        };

        ApplicationResult<UploadedImageResult> result = await this.uploadImageCommandHandler.HandleAsync(
            new UploadImageCommand(image.ToApplication(file)),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("remote")]
    [AdminAudit("image.remote-import", "Image")]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportRemoteAsync([FromBody] RemoteImageCreateDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> result = await this.importRemoteImageCommandHandler.HandleAsync(
            new ImportRemoteImageCommand(request.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("links")]
    [AdminAudit("image.link", "Image")]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LinkImageAsync([FromBody] LinkImageToOwnerDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> result = await this.linkImageCommandHandler.HandleAsync(
            new LinkImageCommand(request.ImageId, request.OwnerType.ToDomain(), request.OwnerId, request.Description, request.SetAsCurrent),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{ownerType}/{ownerId}/{category}/current")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentImageAsync([FromRoute] string ownerType, [FromRoute] string ownerId, [FromRoute] string category, CancellationToken cancellationToken = default)
    {
        if (!ImagesHttpMappers.TryParseImageOwnerTypeDto(ownerType, out ImageOwnerTypeDto parsedOwnerType))
        {
            return this.ToProblemDetailsResult(StatusCodes.Status400BadRequest, "Invalid ownerType.", "image.owner-type-invalid");
        }

        if (!ImagesHttpMappers.TryParseImageCategoryDto(category, out ImageCategoryDto parsedCategory))
        {
            return this.ToProblemDetailsResult(StatusCodes.Status400BadRequest, "Invalid category.", "image.category-invalid");
        }

        ApplicationResult<Image> result = await this.getCurrentImageQueryHandler.HandleAsync(
            new GetCurrentImageQuery(ownerId, parsedOwnerType.ToDomain(), parsedCategory.ToDomain()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{ownerType}/{ownerId}/{category}")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<ImageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImagesAsync([FromRoute] string ownerType, [FromRoute] string ownerId, [FromRoute] string category, [FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        if (!ImagesHttpMappers.TryParseImageOwnerTypeDto(ownerType, out ImageOwnerTypeDto parsedOwnerType))
        {
            return this.ToProblemDetailsResult(StatusCodes.Status400BadRequest, "Invalid ownerType.", "image.owner-type-invalid");
        }

        if (!ImagesHttpMappers.TryParseImageCategoryDto(category, out ImageCategoryDto parsedCategory))
        {
            return this.ToProblemDetailsResult(StatusCodes.Status400BadRequest, "Invalid category.", "image.category-invalid");
        }

        bool canSeeNonVisible = this.UserCanSeeNonVisible();
        ImageSearchCriteria criteria = new ImageSearchCriteria(
            Category: parsedCategory.ToDomain(),
            OwnerType: parsedOwnerType.ToDomain(),
            OwnerId: ownerId,
            IsPublished: canSeeNonVisible ? null : true,
            SortBy: "created",
            SortDirection: "desc");

        ApplicationResult<PagedResult<Image>> result = await this.getImagesPageQueryHandler.HandleAsync(new GetImagesPageQuery(pagination.ToApplication(), criteria), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ImageDto> response = result.Value.ToPagedResponse(static imageValue => imageValue.ToHttp());
        return this.Ok(response);
    }

    [HttpGet("parks/{parkId}/park-items")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<ParkItemImageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParkItemImagesByParkAsync([FromRoute] string parkId, [FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        bool canSeeNonVisible = this.UserCanSeeNonVisible();
        ApplicationResult<PagedResult<ParkItemImageResult>> result = await this.getParkItemImagesByParkQueryHandler.HandleAsync(
            new GetParkItemImagesByParkQuery(pagination.ToApplication(), parkId, canSeeNonVisible),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ParkItemImageDto> response = result.Value.ToPagedResponse(static imageValue => imageValue.ToHttp());
        return this.Ok(response);
    }

    [HttpPut("{imageId}/current")]
    [AdminAudit("image.current.set", "Image", TargetIdRouteKey = "imageId")]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetCurrentImageAsync([FromRoute] string imageId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> result = await this.setCurrentImageCommandHandler.HandleAsync(new SetCurrentImageCommand(imageId, ImageOwnerType.None, string.Empty), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("{imageId}/watermark")]
    [AdminAudit("image.watermark.apply", "Image", TargetIdRouteKey = "imageId")]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyWatermarkAsync([FromRoute] string imageId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> result = await this.applyImageWatermarkCommandHandler.HandleAsync(new ApplyImageWatermarkCommand(imageId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpDelete("{imageId}")]
    [AdminAudit("image.delete", "Image", TargetIdRouteKey = "imageId")]
    public async Task<IActionResult> DeleteImageAsync([FromRoute] string imageId, CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.deleteImageCommandHandler.HandleAsync(new DeleteImageCommand(imageId), cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(true);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<ImageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? search = null,
        [FromQuery] ImageCategoryDto? category = null,
        [FromQuery] ImageOwnerTypeDto? ownerType = null,
        [FromQuery] string? ownerId = null,
        [FromQuery] string? tagId = null,
        [FromQuery] bool? isPublished = null,
        [FromQuery] bool? hasOwner = null,
        [FromQuery] bool? hasGeoLocation = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        ImageSearchCriteria criteria = new ImageSearchCriteria(
            search,
            category.ToOptionalDomain(),
            ownerType.ToOptionalDomain(),
            ownerId,
            tagId,
            isPublished,
            hasOwner,
            hasGeoLocation,
            sortBy,
            sortDirection);

        ApplicationResult<PagedResult<Image>> result = await this.getImagesPageQueryHandler.HandleAsync(new GetImagesPageQuery(pagination.ToApplication(), criteria), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ImageDto> response = result.Value.ToPagedResponse(static imageValue => imageValue.ToHttp());
        return this.Ok(response);
    }

    [HttpPatch("bulk-metadata")]
    [AdminAudit("image.bulk-metadata.update", "Image", StaticTargetId = "bulk")]
    [ProducesResponseType(typeof(BulkAdministrationUpdateResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBulkMetadataAsync([FromBody] BulkImageMetadataUpdateDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<BulkAdministrationUpdateResult> result = await this.updateImagesBulkMetadataCommandHandler.HandleAsync(
            new UpdateImagesBulkMetadataCommand(request.ImageIds, request.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new BulkAdministrationUpdateResultDto
        {
            RequestedCount = result.Value.RequestedCount,
            UpdatedCount = result.Value.UpdatedCount,
        });
    }

    [HttpGet("tags")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicReferenceData)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<ImageTagDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTagsAsync([FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<ImageTag>> result = await this.listImageTagsQueryHandler.HandleAsync(new ListImageTagsQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ImageTagDto> response = pagination.ToPagedResponse(result.Value, static value => value.ToHttp());
        return this.Ok(response);
    }

    [HttpPost("tags")]
    [AdminAudit("image-tag.create", "ImageTag")]
    [ProducesResponseType(typeof(ImageTagDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTagAsync([FromBody] CreateImageTagRequest request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ImageTag> result = await this.createImageTagCommandHandler.HandleAsync(new CreateImageTagCommand(request.ToApplication()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("tags/{id}")]
    [AdminAudit("image-tag.update", "ImageTag", TargetIdRouteKey = "id")]
    [ProducesResponseType(typeof(ImageTagDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTagAsync([FromRoute] string id, [FromBody] UpdateImageTagRequest request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ImageTag> result = await this.updateImageTagCommandHandler.HandleAsync(new UpdateImageTagCommand(id, request.ToApplication()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{imageId}/metadata")]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetadataAsync([FromRoute] string imageId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> result = await this.getImageByIdQueryHandler.HandleAsync(new GetImageByIdQuery(imageId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{imageId}/metadata")]
    [AdminAudit("image.metadata.update", "Image", TargetIdRouteKey = "imageId")]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMetadataAsync([FromRoute] string imageId, [FromBody] UpdateImageAssetRequest request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> existingResult = await this.getImageByIdQueryHandler.HandleAsync(new GetImageByIdQuery(imageId), cancellationToken);
        if (!existingResult.IsSuccess || existingResult.Value is null)
        {
            return this.ToActionResult(existingResult);
        }

        ApplicationResult<Image> result = await this.updateImageMetadataCommandHandler.HandleAsync(
            new UpdateImageMetadataCommand(imageId, request.ToApplication(existingResult.Value)),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("binary/{imageId}")]
    [HttpHead("binary/{imageId}")]
    [HttpGet("{imageId}")]
    [HttpHead("{imageId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetImageAsync([FromRoute] string imageId, [FromQuery] int? width, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> result = await this.getImageByIdQueryHandler.HandleAsync(new GetImageByIdQuery(imageId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        if (string.IsNullOrWhiteSpace(result.Value.Path))
        {
            return this.ToNotFoundProblemDetailsResult("The requested image binary was not found.", "image.binary-not-found");
        }

        (System.IO.Stream Stream, string ContentType)? binary = await this.imageBinaryStorage.GetBestAsync(result.Value.Path, this.Request.Headers.Accept.ToString(), width, cancellationToken);
        if (binary is null)
        {
            return this.ToNotFoundProblemDetailsResult("The requested image binary was not found.", "image.binary-not-found");
        }

        this.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        this.Response.Headers.Vary = "Accept";
        return this.File(binary.Value.Stream, binary.Value.ContentType);
    }

    private bool UserCanSeeNonVisible()
    {
        return this.HttpContext.UserCanSeeNonVisibleInPublicView();
    }
}
