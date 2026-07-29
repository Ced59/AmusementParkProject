using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Users;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class UserAvatarsControllerTests
{
    [Fact]
    public void Controller_ShouldAllowEveryAuthenticatedApplicationRoleWithoutOpeningImagesAdministration()
    {
        AuthorizeAttribute authorize = typeof(UserAvatarsController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .SingleOrDefault(static attribute => !string.IsNullOrWhiteSpace(attribute.Roles))
            ?? throw new InvalidOperationException("UserAvatarsController has no role authorization policy.");
        MethodInfo method = typeof(UserAvatarsController).GetMethod(nameof(UserAvatarsController.UploadCurrentUserAvatarAsync))
            ?? throw new InvalidOperationException("The current-user avatar endpoint was not found.");
        EnableRateLimitingAttribute rateLimit = method.GetCustomAttribute<EnableRateLimitingAttribute>()
            ?? throw new InvalidOperationException("The current-user avatar endpoint has no rate limit.");

        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.Equal(RateLimitPolicyNames.ImageUploadProcessing, rateLimit.PolicyName);
        RequestSizeLimitAttribute requestSizeLimit = method.GetCustomAttribute<RequestSizeLimitAttribute>()
            ?? throw new InvalidOperationException("The current-user avatar endpoint has no request-size limit.");
        CustomAttributeData requestSizeLimitData = method.CustomAttributes
            .Single(attribute => attribute.AttributeType == typeof(RequestSizeLimitAttribute));
        long requestSizeLimitBytes = Assert.IsType<long>(
            requestSizeLimitData.ConstructorArguments.Single().Value);
        Assert.InRange(requestSizeLimitBytes, 5 * 1024 * 1024, 6 * 1024 * 1024);
        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task UploadCurrentUserAvatarAsync_ShouldForceAuthenticatedUserAvatarOwnership()
    {
        Image uploadedImage = new Image
        {
            Id = "avatar-1",
            Category = ImageCategory.Avatar,
            OwnerType = ImageOwnerType.User,
            OwnerId = "user-1",
        };
        Mock<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>> uploadHandler =
            new Mock<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>>(MockBehavior.Strict);
        uploadHandler
            .Setup(handler => handler.HandleAsync(
                It.Is<UploadImageCommand>(command =>
                    command.Request.Category == ImageCategory.Avatar
                    && command.Request.OwnerType == ImageOwnerType.User
                    && command.Request.OwnerId == "user-1"
                    && !command.Request.WithWatermark
                    && command.Request.File.FileName == "avatar.png"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<UploadedImageResult>.Success(new UploadedImageResult
            {
                Image = uploadedImage,
                SavedFiles = Array.Empty<string>(),
            }));
        Mock<ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>>> setCurrentHandler =
            new Mock<ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>>>(MockBehavior.Strict);
        setCurrentHandler
            .Setup(handler => handler.HandleAsync(
                new SetCurrentImageCommand("avatar-1", ImageOwnerType.User, "user-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<Image>.Success(uploadedImage));
        UserAvatarsController controller = CreateController(uploadHandler, setCurrentHandler, "user-1");
        FormFile file = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "File", "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };

        IActionResult result = await controller.UploadCurrentUserAvatarAsync(new UserAvatarUploadDto { File = file });

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        uploadHandler.VerifyAll();
        setCurrentHandler.VerifyAll();
    }

    [Fact]
    public async Task UploadCurrentUserAvatarAsync_WithoutUserIdentifier_ShouldRejectBeforeUploading()
    {
        Mock<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>> uploadHandler =
            new Mock<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>>(MockBehavior.Strict);
        Mock<ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>>> setCurrentHandler =
            new Mock<ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>>>(MockBehavior.Strict);
        UserAvatarsController controller = CreateController(uploadHandler, setCurrentHandler, null);

        IActionResult result = await controller.UploadCurrentUserAvatarAsync(new UserAvatarUploadDto());

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        uploadHandler.VerifyNoOtherCalls();
        setCurrentHandler.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, "image/png")]
    [InlineData(5242881, "image/png")]
    [InlineData(3, "image/svg+xml")]
    public async Task UploadCurrentUserAvatarAsync_WithUnsafeFile_ShouldRejectBeforeProcessing(
        long fileLength,
        string contentType)
    {
        Mock<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>> uploadHandler =
            new Mock<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>>(MockBehavior.Strict);
        Mock<ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>>> setCurrentHandler =
            new Mock<ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>>>(MockBehavior.Strict);
        UserAvatarsController controller = CreateController(uploadHandler, setCurrentHandler, "user-1");
        FormFile file = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, fileLength, "File", "avatar")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };

        IActionResult result = await controller.UploadCurrentUserAvatarAsync(
            new UserAvatarUploadDto { File = file });

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        uploadHandler.VerifyNoOtherCalls();
        setCurrentHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadCurrentUserAvatarAsync_WhenUserQuotaIsExceeded_ShouldRejectBeforeProcessing()
    {
        Mock<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>> uploadHandler =
            new Mock<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>>(MockBehavior.Strict);
        Mock<ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>>> setCurrentHandler =
            new Mock<ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>>>(MockBehavior.Strict);
        Mock<IAvatarUploadAttemptLimiter> attemptLimiter =
            new Mock<IAvatarUploadAttemptLimiter>(MockBehavior.Strict);
        attemptLimiter
            .Setup(limiter => limiter.TryAcquire("user-1", It.IsAny<DateTime>()))
            .Returns(new AvatarUploadAttemptLease(false, TimeSpan.FromMinutes(5)));
        UserAvatarsController controller = CreateController(
            uploadHandler,
            setCurrentHandler,
            "user-1",
            attemptLimiter);
        FormFile file = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "File", "avatar.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg",
        };

        IActionResult result = await controller.UploadCurrentUserAvatarAsync(
            new UserAvatarUploadDto { File = file });

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
        Assert.True(controller.Response.Headers.ContainsKey("Retry-After"));
        attemptLimiter.VerifyAll();
        uploadHandler.VerifyNoOtherCalls();
        setCurrentHandler.VerifyNoOtherCalls();
    }

    private static UserAvatarsController CreateController(
        Mock<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>> uploadHandler,
        Mock<ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>>> setCurrentHandler,
        string? userId,
        Mock<IAvatarUploadAttemptLimiter>? attemptLimiter = null)
    {
        List<Claim> claims = new List<Claim>();
        if (userId is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        Mock<IAvatarUploadAttemptLimiter> resolvedAttemptLimiter = attemptLimiter
            ?? new Mock<IAvatarUploadAttemptLimiter>(MockBehavior.Strict);
        if (attemptLimiter is null)
        {
            resolvedAttemptLimiter
                .Setup(limiter => limiter.TryAcquire(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(new AvatarUploadAttemptLease(true, TimeSpan.Zero));
        }

        UserAvatarsController controller = new UserAvatarsController(
            uploadHandler.Object,
            setCurrentHandler.Object,
            resolvedAttemptLimiter.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
                },
            },
        };
        return controller;
    }
}
