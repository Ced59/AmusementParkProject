using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature Images migrée en phase 9.
/// </summary>
[ApiController]
[Route("images")]
public sealed class ImagesController : ControllerBase
{
    private readonly ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>> uploadImageCommandHandler;
    private readonly ICommandHandler<LinkImageCommand, ApplicationResult<Image>> linkImageCommandHandler;
    private readonly ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>> setCurrentImageCommandHandler;
    private readonly ICommandHandler<DeleteImageCommand, ApplicationResult> deleteImageCommandHandler;
    private readonly ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>> updateImageMetadataCommandHandler;
    private readonly ICommandHandler<CreateImageTagCommand, ApplicationResult<ImageTag>> createImageTagCommandHandler;
    private readonly ICommandHandler<UpdateImageTagCommand, ApplicationResult<ImageTag>> updateImageTagCommandHandler;
    private readonly IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>> getImageByIdQueryHandler;
    private readonly IQueryHandler<GetAllImagesQuery, ApplicationResult<IReadOnlyCollection<Image>>> getAllImagesQueryHandler;
    private readonly IQueryHandler<GetCurrentImageQuery, ApplicationResult<Image>> getCurrentImageQueryHandler;
    private readonly IQueryHandler<GetImagesByOwnerQuery, ApplicationResult<IReadOnlyCollection<Image>>> getImagesByOwnerQueryHandler;
    private readonly IQueryHandler<ListImageTagsQuery, ApplicationResult<IReadOnlyCollection<ImageTag>>> listImageTagsQueryHandler;
    private readonly IImageBinaryStorage imageBinaryStorage;

    public ImagesController(
        ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>> uploadImageCommandHandler,
        ICommandHandler<LinkImageCommand, ApplicationResult<Image>> linkImageCommandHandler,
        ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>> setCurrentImageCommandHandler,
        ICommandHandler<DeleteImageCommand, ApplicationResult> deleteImageCommandHandler,
        ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>> updateImageMetadataCommandHandler,
        ICommandHandler<CreateImageTagCommand, ApplicationResult<ImageTag>> createImageTagCommandHandler,
        ICommandHandler<UpdateImageTagCommand, ApplicationResult<ImageTag>> updateImageTagCommandHandler,
        IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>> getImageByIdQueryHandler,
        IQueryHandler<GetAllImagesQuery, ApplicationResult<IReadOnlyCollection<Image>>> getAllImagesQueryHandler,
        IQueryHandler<GetCurrentImageQuery, ApplicationResult<Image>> getCurrentImageQueryHandler,
        IQueryHandler<GetImagesByOwnerQuery, ApplicationResult<IReadOnlyCollection<Image>>> getImagesByOwnerQueryHandler,
        IQueryHandler<ListImageTagsQuery, ApplicationResult<IReadOnlyCollection<ImageTag>>> listImageTagsQueryHandler,
        IImageBinaryStorage imageBinaryStorage)
    {
        this.uploadImageCommandHandler = uploadImageCommandHandler;
        this.linkImageCommandHandler = linkImageCommandHandler;
        this.setCurrentImageCommandHandler = setCurrentImageCommandHandler;
        this.deleteImageCommandHandler = deleteImageCommandHandler;
        this.updateImageMetadataCommandHandler = updateImageMetadataCommandHandler;
        this.createImageTagCommandHandler = createImageTagCommandHandler;
        this.updateImageTagCommandHandler = updateImageTagCommandHandler;
        this.getImageByIdQueryHandler = getImageByIdQueryHandler;
        this.getAllImagesQueryHandler = getAllImagesQueryHandler;
        this.getCurrentImageQueryHandler = getCurrentImageQueryHandler;
        this.getImagesByOwnerQueryHandler = getImagesByOwnerQueryHandler;
        this.listImageTagsQueryHandler = listImageTagsQueryHandler;
        this.imageBinaryStorage = imageBinaryStorage;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageCreatedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadAsync([FromForm] ImageCreateDto image, CancellationToken cancellationToken = default)
    {
        if (image.File is null)
        {
            return this.BadRequest(new { StatusCode = 400, Message = "No image filename provided." });
        }

        await using Stream content = image.File.OpenReadStream();
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

    [HttpPost("links")]
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
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentImageAsync([FromRoute] string ownerType, [FromRoute] string ownerId, [FromRoute] string category, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<ImageOwnerTypeDto>(ownerType, true, out ImageOwnerTypeDto parsedOwnerType))
        {
            return this.BadRequest("Invalid ownerType.");
        }

        if (!Enum.TryParse<ImageCategoryDto>(category, true, out ImageCategoryDto parsedCategory))
        {
            return this.BadRequest("Invalid category.");
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
    [ProducesResponseType(typeof(IEnumerable<ImageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImagesAsync([FromRoute] string ownerType, [FromRoute] string ownerId, [FromRoute] string category, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<ImageOwnerTypeDto>(ownerType, true, out ImageOwnerTypeDto parsedOwnerType))
        {
            return this.BadRequest("Invalid ownerType.");
        }

        if (!Enum.TryParse<ImageCategoryDto>(category, true, out ImageCategoryDto parsedCategory))
        {
            return this.BadRequest("Invalid category.");
        }

        ApplicationResult<IReadOnlyCollection<Image>> result = await this.getImagesByOwnerQueryHandler.HandleAsync(
            new GetImagesByOwnerQuery(ownerId, parsedOwnerType.ToDomain(), parsedCategory.ToDomain()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        List<ImageDto> response = result.Value.Select(static imageValue => imageValue.ToHttp()).ToList();
        return this.Ok(response);
    }

    [HttpPut("{imageId}/current")]
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

    [HttpDelete("{imageId}")]
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
    [ProducesResponseType(typeof(IEnumerable<ImageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<Image>> result = await this.getAllImagesQueryHandler.HandleAsync(new GetAllImagesQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        List<ImageDto> response = result.Value.Select(static imageValue => imageValue.ToHttp()).ToList();
        return this.Ok(response);
    }

    [HttpGet("tags")]
    [ProducesResponseType(typeof(IEnumerable<ImageTagDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<ImageTag>> result = await this.listImageTagsQueryHandler.HandleAsync(new ListImageTagsQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        List<ImageTagDto> response = result.Value.Select(static value => value.ToHttp()).ToList();
        return this.Ok(response);
    }

    [HttpPost("tags")]
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
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMetadataAsync([FromRoute] string imageId, [FromBody] UpdateImageAssetRequest request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> existingResult = await this.getImageByIdQueryHandler.HandleAsync(new GetImageByIdQuery(imageId), cancellationToken);
        if (!existingResult.IsSuccess || existingResult.Value is null)
        {
            return this.ToActionResult(existingResult);
        }

        ApplicationResult<Image> result = await this.updateImageMetadataCommandHandler.HandleAsync(
            new UpdateImageMetadataCommand(imageId, request.ToApplication(existingResult.Value.Category)),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{imageId}")]
    public async Task<IActionResult> GetImageAsync([FromRoute] string imageId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> result = await this.getImageByIdQueryHandler.HandleAsync(new GetImageByIdQuery(imageId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        if (string.IsNullOrWhiteSpace(result.Value.Path))
        {
            return this.NotFound();
        }

        (Stream Stream, string ContentType)? binary = await this.imageBinaryStorage.GetBestAsync(result.Value.Path, this.Request.Headers.Accept.ToString(), cancellationToken);
        if (binary is null)
        {
            return this.NotFound();
        }

        return this.File(binary.Value.Stream, binary.Value.ContentType);
    }
}
