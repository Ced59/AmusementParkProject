using System.Reflection;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ImagesControllerTests
{
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
