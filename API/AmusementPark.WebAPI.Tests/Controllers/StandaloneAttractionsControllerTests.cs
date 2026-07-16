using System.Reflection;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class StandaloneAttractionsControllerTests
{
    [Fact]
    public void GetAdminPaginatedAsync_ShouldExposeDedicatedUncachedAdminRoute()
    {
        MethodInfo method = typeof(StandaloneAttractionsController).GetMethod(nameof(StandaloneAttractionsController.GetAdminPaginatedAsync))
            ?? throw new InvalidOperationException("StandaloneAttractionsController.GetAdminPaginatedAsync was not found.");

        HttpGetAttribute httpGetAttribute = method.GetCustomAttribute<HttpGetAttribute>()
            ?? throw new InvalidOperationException("HttpGetAttribute was not found.");
        ResponseCacheAttribute responseCacheAttribute = method.GetCustomAttribute<ResponseCacheAttribute>()
            ?? throw new InvalidOperationException("ResponseCacheAttribute was not found.");

        Assert.Equal("~/admin/standalone-attractions", httpGetAttribute.Template);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Null(method.GetCustomAttribute<OutputCacheAttribute>());
        Assert.True(responseCacheAttribute.NoStore);
        Assert.Equal(ResponseCacheLocation.None, responseCacheAttribute.Location);
    }

    [Fact]
    public void GetAdminByIdAsync_ShouldExposeDedicatedUncachedAdminRoute()
    {
        MethodInfo method = typeof(StandaloneAttractionsController).GetMethod(nameof(StandaloneAttractionsController.GetAdminByIdAsync))
            ?? throw new InvalidOperationException("StandaloneAttractionsController.GetAdminByIdAsync was not found.");

        HttpGetAttribute httpGetAttribute = method.GetCustomAttribute<HttpGetAttribute>()
            ?? throw new InvalidOperationException("HttpGetAttribute was not found.");
        ResponseCacheAttribute responseCacheAttribute = method.GetCustomAttribute<ResponseCacheAttribute>()
            ?? throw new InvalidOperationException("ResponseCacheAttribute was not found.");

        Assert.Equal("~/admin/standalone-attractions/{id}", httpGetAttribute.Template);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Null(method.GetCustomAttribute<OutputCacheAttribute>());
        Assert.True(responseCacheAttribute.NoStore);
        Assert.Equal(ResponseCacheLocation.None, responseCacheAttribute.Location);
    }
}
