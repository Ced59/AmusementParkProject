using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.Users;
using AmusementPark.WebAPI.Extensions;
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
/// Gère l'avatar du compte connecté sans exposer les opérations d'administration des images.
/// </summary>
[ApiController]
[Route("users/me/avatar")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
public sealed class UserAvatarsController : ControllerBase
{
    private readonly ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>> uploadImageCommandHandler;
    private readonly ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>> setCurrentImageCommandHandler;

    public UserAvatarsController(
        ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>> uploadImageCommandHandler,
        ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>> setCurrentImageCommandHandler)
    {
        this.uploadImageCommandHandler = uploadImageCommandHandler;
        this.setCurrentImageCommandHandler = setCurrentImageCommandHandler;
    }

    [HttpPost]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [EnableRateLimiting(RateLimitPolicyNames.ImageUploadProcessing)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadCurrentUserAvatarAsync(
        [FromForm] UserAvatarUploadDto request,
        CancellationToken cancellationToken = default)
    {
        string? currentUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status401Unauthorized,
                "The authenticated user identifier is missing.",
                "user.identifier-missing");
        }

        if (request.File is null)
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status400BadRequest,
                "No image file was provided.",
                "image.file-required");
        }

        await using System.IO.Stream content = request.File.OpenReadStream();
        FilePayload file = new FilePayload
        {
            FileName = request.File.FileName,
            ContentType = request.File.ContentType,
            Length = request.File.Length,
            Content = content,
        };

        ImageUploadRequest uploadRequest = new ImageUploadRequest
        {
            Category = ImageCategory.Avatar,
            File = file,
            WithWatermark = false,
            OwnerType = ImageOwnerType.User,
            OwnerId = currentUserId.Trim(),
        };
        ApplicationResult<UploadedImageResult> uploadResult = await this.uploadImageCommandHandler.HandleAsync(
            new UploadImageCommand(uploadRequest),
            cancellationToken);
        if (!uploadResult.IsSuccess || uploadResult.Value is null)
        {
            return this.ToActionResult(uploadResult);
        }

        ApplicationResult<Image> currentResult = await this.setCurrentImageCommandHandler.HandleAsync(
            new SetCurrentImageCommand(
                uploadResult.Value.Image.Id,
                ImageOwnerType.User,
                currentUserId.Trim()),
            cancellationToken);
        if (!currentResult.IsSuccess || currentResult.Value is null)
        {
            return this.ToActionResult(currentResult);
        }

        return this.Ok(currentResult.Value.ToHttp());
    }
}
