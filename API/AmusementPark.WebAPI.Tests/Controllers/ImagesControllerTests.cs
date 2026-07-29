using System.Reflection;
using System.Security.Claims;
using AmusementPark.Core.Domain.Images;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ImagesControllerTests
{
    [Theory]
    [InlineData(null, null, false)]
    [InlineData("other-user", "MODERATOR", false)]
    [InlineData("draft-owner", "USER", false)]
    [InlineData("draft-owner", "MODERATOR", true)]
    [InlineData("draft-owner", "ADMIN", true)]
    public void DraftBinary_ShouldOnlyBeReadableByItsStaffOwner(
        string? userId,
        string? role,
        bool expected)
    {
        List<Claim> claims = new List<Claim>();
        if (userId is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        if (role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        ClaimsPrincipal user = new ClaimsPrincipal(
            new ClaimsIdentity(claims, userId is null ? null : "test"));
        Image draft = new Image
        {
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "draft-owner",
            IsPublished = false,
        };

        Assert.Equal(expected, ImagesController.CanReadOwnCommentDraft(draft, user));
    }

    [Theory]
    [InlineData(ImageOwnerTypeDto.COMMENT, ImageCategoryDto.PARK)]
    [InlineData(ImageOwnerTypeDto.COMMENT_DRAFT, ImageCategoryDto.PARK)]
    [InlineData(ImageOwnerTypeDto.PARK, ImageCategoryDto.COMMENT)]
    public void AnonymousOwnerRoutes_WhenCommentImageIsRequested_ShouldBeHidden(
        ImageOwnerTypeDto ownerType,
        ImageCategoryDto category)
    {
        Assert.True(ImagesController.IsCommentImageOwnerRoute(ownerType, category));
    }

    [Fact]
    public void AnonymousOwnerRoutes_WhenPublicParkImageIsRequested_ShouldRemainAvailable()
    {
        Assert.False(ImagesController.IsCommentImageOwnerRoute(
            ImageOwnerTypeDto.PARK,
            ImageCategoryDto.PARK));
    }

    [Fact]
    public void GetImageAsync_ShouldExposeAnonymousGetAndHeadRoutes()
    {
        MethodInfo method = typeof(ImagesController).GetMethod(nameof(ImagesController.GetImageAsync))
            ?? throw new InvalidOperationException("ImagesController.GetImageAsync was not found.");

        Assert.Contains(method.GetCustomAttributes<HttpGetAttribute>(), static attribute => attribute.Template == "{imageId}");
        Assert.Contains(method.GetCustomAttributes<HttpHeadAttribute>(), static attribute => attribute.Template == "{imageId}");
        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(nameof(ImagesController.UploadAsync))]
    [InlineData(nameof(ImagesController.ImportRemoteAsync))]
    public void ImageImportEndpoints_ShouldUseTheSharedProcessingQueue(string methodName)
    {
        MethodInfo method = typeof(ImagesController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"ImagesController.{methodName} was not found.");

        EnableRateLimitingAttribute attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>()
            ?? throw new InvalidOperationException($"ImagesController.{methodName} has no rate limiting policy.");

        Assert.Equal(RateLimitPolicyNames.ImageUploadProcessing, attribute.PolicyName);
    }
}
