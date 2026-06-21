using System.Security.Claims;
using AmusementPark.WebAPI.AdminPublicView;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.AdminPublicView;

public sealed class AdminPublicViewSimulationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenHeaderIsInvalid_ShouldRejectRequestWithNoStore()
    {
        bool nextCalled = false;
        DefaultHttpContext context = CreateContext();
        context.Request.Headers[AdminPublicViewSimulation.RequestHeaderName] = "owner";
        AdminPublicViewSimulationMiddleware middleware = new AdminPublicViewSimulationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("no-store", context.Response.Headers.CacheControl.ToString());
        Assert.Contains(AdminPublicViewSimulation.RequestHeaderName, context.Response.Headers.Vary.ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenRequesterIsNotAdmin_ShouldRejectSimulation()
    {
        bool nextCalled = false;
        DefaultHttpContext context = CreateContext();
        context.User = CreatePrincipal("USER");
        context.Request.Headers[AdminPublicViewSimulation.RequestHeaderName] = "userVisitor";
        AdminPublicViewSimulationMiddleware middleware = new AdminPublicViewSimulationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("no-store", context.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenRequesterIsAdmin_ShouldApplyModeAndNoStore()
    {
        bool nextCalled = false;
        DefaultHttpContext context = CreateContext();
        context.User = CreatePrincipal("ADMIN");
        context.Request.Headers[AdminPublicViewSimulation.RequestHeaderName] = "moderatorVisitor";
        AdminPublicViewSimulationMiddleware middleware = new AdminPublicViewSimulationMiddleware(async httpContext =>
        {
            nextCalled = true;
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            await httpContext.Response.StartAsync();
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("no-store", context.Response.Headers.CacheControl.ToString());
        Assert.Equal("moderatorVisitor", context.Response.Headers[AdminPublicViewSimulation.AppliedResponseHeaderName].ToString());
        Assert.Equal(AdminPublicViewSimulationMode.ModeratorVisitor, AdminPublicViewSimulation.GetAppliedMode(context));
    }

    [Fact]
    public void CanSeeNonVisible_WhenNoSimulation_ShouldKeepRealAdminBehavior()
    {
        DefaultHttpContext context = CreateContext();
        context.User = CreatePrincipal("ADMIN");

        bool canSeeNonVisible = AdminPublicViewSimulation.CanSeeNonVisible(context);

        Assert.True(canSeeNonVisible);
    }

    [Fact]
    public void CanSeeNonVisible_WhenAdminSimulatesUser_ShouldHideNonVisibleContent()
    {
        DefaultHttpContext context = CreateContext();
        context.User = CreatePrincipal("ADMIN");
        AdminPublicViewSimulation.SetAppliedMode(context, AdminPublicViewSimulationMode.UserVisitor);

        bool canSeeNonVisible = AdminPublicViewSimulation.CanSeeNonVisible(context);

        Assert.False(canSeeNonVisible);
    }

    [Fact]
    public void CanSeeNonVisible_WhenAdminSimulatesAdminPreview_ShouldShowNonVisibleContent()
    {
        DefaultHttpContext context = CreateContext();
        context.User = CreatePrincipal("ADMIN");
        AdminPublicViewSimulation.SetAppliedMode(context, AdminPublicViewSimulationMode.AdminPreview);

        bool canSeeNonVisible = AdminPublicViewSimulation.CanSeeNonVisible(context);

        Assert.True(canSeeNonVisible);
    }

    private static DefaultHttpContext CreateContext()
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/parks/park-1";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static ClaimsPrincipal CreatePrincipal(string role)
    {
        ClaimsIdentity identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, role),
        }, "Test");

        return new ClaimsPrincipal(identity);
    }
}
