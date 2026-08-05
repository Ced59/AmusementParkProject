using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Surface technique Codex pour téléverser un fichier déjà téléchargé et le rattacher
/// uniquement à une donnée de parc. Le workflow d'import distant existant reste séparé.
/// </summary>
[ApiController]
[Route("park-data-editor/images")]
[Authorize(Policy = AuthorizationPolicyNames.ParkDataEditorToken)]
[AllowParkDataEditorToken]
[RequireActivatedUnblockedUser]
[InvalidatesPublicCache(PublicCacheScope.Data, PublicCacheScope.ReferenceData)]
public sealed class ParkDataEditorImagesController : ControllerBase
{
    private const long MaximumImageFileSizeInBytes = 10 * 1024 * 1024;
    private readonly ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>> uploadHandler;
    private readonly ICommandHandler<LinkImageCommand, ApplicationResult<Image>> linkHandler;
    private readonly ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>> setCurrentHandler;
    private readonly ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>> updateMetadataHandler;
    private readonly IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>> getImageHandler;

    public ParkDataEditorImagesController(
        ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>> uploadHandler,
        ICommandHandler<LinkImageCommand, ApplicationResult<Image>> linkHandler,
        ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>> setCurrentHandler,
        ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>> updateMetadataHandler,
        IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>> getImageHandler)
    {
        this.uploadHandler = uploadHandler;
        this.linkHandler = linkHandler;
        this.setCurrentHandler = setCurrentHandler;
        this.updateMetadataHandler = updateMetadataHandler;
        this.getImageHandler = getImageHandler;
    }

    [HttpPost]
    [AdminAudit("park-data-editor.image.upload", "Image")]
    [EnableRateLimiting(RateLimitPolicyNames.ImageUploadProcessing)]
    [RequestSizeLimit(MaximumImageFileSizeInBytes + (64 * 1024))]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageCreatedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadAsync(
        [FromForm] ParkDataEditorImageCreateDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.File is null || request.File.Length <= 0 || request.File.Length > MaximumImageFileSizeInBytes)
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status400BadRequest,
                "An image file between 1 byte and 10 MB is required.",
                "image.file-invalid");
        }

        ImageCategory category = request.Category.ToDomain();
        if (!IsAllowedCategory(category))
        {
            return this.ScopeDenied();
        }

        await using Stream content = request.File.OpenReadStream();
        FilePayload file = new FilePayload
        {
            FileName = request.File.FileName,
            ContentType = request.File.ContentType,
            Length = request.File.Length,
            Content = content,
        };
        ApplicationResult<UploadedImageResult> result = await this.uploadHandler.HandleAsync(
            new UploadImageCommand(request.ToApplication(file)),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("links")]
    [AdminAudit("park-data-editor.image.link", "Image")]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LinkAsync(
        [FromBody] LinkImageToOwnerDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> imageResult = await this.getImageHandler.HandleAsync(
            new GetImageByIdQuery(request.ImageId),
            cancellationToken);
        if (!imageResult.IsSuccess || imageResult.Value is null)
        {
            return this.ToActionResult(imageResult);
        }

        ImageOwnerType ownerType = request.OwnerType.ToDomain();
        if (!IsAllowedImageScope(imageResult.Value)
            || !IsAllowedOwnership(imageResult.Value.Category, ownerType, request.OwnerId))
        {
            return this.ScopeDenied();
        }

        ApplicationResult<Image> result = await this.linkHandler.HandleAsync(
            new LinkImageCommand(
                request.ImageId,
                ownerType,
                request.OwnerId,
                request.Description,
                request.SetAsCurrent),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{imageId}/current")]
    [AdminAudit("park-data-editor.image.current.set", "Image", TargetIdRouteKey = "imageId")]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetCurrentAsync(
        [FromRoute] string imageId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> imageResult = await this.getImageHandler.HandleAsync(
            new GetImageByIdQuery(imageId),
            cancellationToken);
        if (!imageResult.IsSuccess || imageResult.Value is null)
        {
            return this.ToActionResult(imageResult);
        }

        if (!IsAllowedOwnership(
                imageResult.Value.Category,
                imageResult.Value.OwnerType,
                imageResult.Value.OwnerId))
        {
            return this.ScopeDenied();
        }

        ApplicationResult<Image> result = await this.setCurrentHandler.HandleAsync(
            new SetCurrentImageCommand(imageId, ImageOwnerType.None, string.Empty),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{imageId}/metadata")]
    [AdminAudit("park-data-editor.image.metadata.read", "Image", TargetIdRouteKey = "imageId")]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetadataAsync(
        [FromRoute] string imageId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> result = await this.getImageHandler.HandleAsync(
            new GetImageByIdQuery(imageId),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        if (!IsAllowedImageScope(result.Value))
        {
            return this.ScopeDenied();
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{imageId}/metadata")]
    [AdminAudit("park-data-editor.image.metadata.update", "Image", TargetIdRouteKey = "imageId")]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMetadataAsync(
        [FromRoute] string imageId,
        [FromBody] UpdateImageAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> imageResult = await this.getImageHandler.HandleAsync(
            new GetImageByIdQuery(imageId),
            cancellationToken);
        if (!imageResult.IsSuccess || imageResult.Value is null)
        {
            return this.ToActionResult(imageResult);
        }

        if (!IsAllowedImageScope(imageResult.Value))
        {
            return this.ScopeDenied();
        }

        ImageCategory category = request.Category?.ToDomain() ?? imageResult.Value.Category;
        ImageOwnerType ownerType = request.OwnerType?.ToDomain() ?? imageResult.Value.OwnerType;
        string? ownerId = request.OwnerType.HasValue ? request.OwnerId : imageResult.Value.OwnerId;
        if (!IsAllowedCategory(category)
            || (ownerType != ImageOwnerType.None && !IsAllowedOwnership(category, ownerType, ownerId)))
        {
            return this.ScopeDenied();
        }

        ApplicationResult<Image> result = await this.updateMetadataHandler.HandleAsync(
            new UpdateImageMetadataCommand(imageId, request.ToApplication(imageResult.Value)),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    internal static bool IsAllowedCategory(ImageCategory category)
    {
        return category is ImageCategory.Logo
            or ImageCategory.Park
            or ImageCategory.ParkItem
            or ImageCategory.StandaloneAttraction;
    }

    internal static bool IsAllowedImageScope(Image image)
    {
        return IsAllowedCategory(image.Category)
            && (image.OwnerType == ImageOwnerType.None
                || IsAllowedOwnership(image.Category, image.OwnerType, image.OwnerId));
    }

    internal static bool IsAllowedOwnership(
        ImageCategory category,
        ImageOwnerType ownerType,
        string? ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return false;
        }

        return category switch
        {
            ImageCategory.Logo or ImageCategory.Park => ownerType == ImageOwnerType.Park,
            ImageCategory.ParkItem => ownerType == ImageOwnerType.ParkItem,
            ImageCategory.StandaloneAttraction => ownerType == ImageOwnerType.StandaloneAttraction,
            _ => false,
        };
    }

    private IActionResult ScopeDenied()
    {
        return this.ToProblemDetailsResult(
            StatusCodes.Status403Forbidden,
            "The park data editor token cannot manage this image category or owner.",
            "park-data-editor.image-scope-denied");
    }
}
