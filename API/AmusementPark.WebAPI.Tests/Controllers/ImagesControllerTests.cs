using System.Reflection;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
