using System.Security.Claims;
using System.Text.Json;
using AmusementPark.WebAPI.Security;
using AmusementPark.WebAPI.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Security;

public sealed class ParkDataEditorOperationCoordinationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenRequestIsUnauthenticated_ShouldBypassCoordination()
    {
        bool nextWasCalled = false;
        ParkDataEditorOperationCoordinator coordinator = new ParkDataEditorOperationCoordinator();
        using ParkDataEditorOperationLease activeOperation = coordinator.TryBeginRequest(
            "token-a",
            ParkDataEditorOperationKind.ResourceIntensive,
            "POST",
            "/admin/park-graph-upserts/preview")!;
        ParkDataEditorOperationCoordinationMiddleware middleware = new ParkDataEditorOperationCoordinationMiddleware(
            _ =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            });
        DefaultHttpContext context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/admin/park-graph-upserts/apply";
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(
                new ParkDataEditorOperationAttribute(ParkDataEditorOperationKind.ResourceIntensive)),
            "park-data-editor-apply"));

        await middleware.InvokeAsync(context, coordinator);

        Assert.True(nextWasCalled);
        Assert.Equal(1, coordinator.GetSnapshot("token-a").ActiveRequestCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenResourceIntensiveCapacityIsBusy_ShouldReturnRetryableProblem()
    {
        bool nextWasCalled = false;
        ParkDataEditorOperationCoordinator coordinator = new ParkDataEditorOperationCoordinator();
        using ParkDataEditorOperationLease activeOperation = coordinator.TryBeginRequest(
            "token-a",
            ParkDataEditorOperationKind.ResourceIntensive,
            "POST",
            "/admin/park-graph-upserts/preview")!;
        ParkDataEditorOperationCoordinationMiddleware middleware = new ParkDataEditorOperationCoordinationMiddleware(
            _ =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            });
        DefaultHttpContext context = CreateTokenContext("token-b", HttpMethods.Post, "/admin/park-graph-upserts/apply");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, coordinator);

        Assert.False(nextWasCalled);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.Equal("5", context.Response.Headers.RetryAfter);
        context.Response.Body.Position = 0;
        using JsonDocument response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(
            "park-data-editor.operation-busy",
            response.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenEndpointSkipsCoordination_ShouldNotCountOrRejectIt()
    {
        bool nextWasCalled = false;
        ParkDataEditorOperationCoordinator coordinator = new ParkDataEditorOperationCoordinator();
        using ParkDataEditorOperationLease activeOperation = coordinator.TryBeginRequest(
            "token-a",
            ParkDataEditorOperationKind.ResourceIntensive,
            "POST",
            "/admin/park-graph-upserts/preview")!;
        ParkDataEditorOperationCoordinationMiddleware middleware = new ParkDataEditorOperationCoordinationMiddleware(
            _ =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            });
        DefaultHttpContext context = CreateTokenContext(
            "token-b",
            HttpMethods.Get,
            "/park-data-editor/operations/status");
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new SkipParkDataEditorOperationCoordinationAttribute()),
            "operation-status"));

        await middleware.InvokeAsync(context, coordinator);

        Assert.True(nextWasCalled);
        Assert.Equal(1, coordinator.GetSnapshot("token-b").ActiveRequestCount);
    }

    private static DefaultHttpContext CreateTokenContext(string tokenId, string method, string path)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(
                ParkDataEditorAuthenticationDefaults.AuthenticationMethodClaim,
                ParkDataEditorAuthenticationDefaults.AuthenticationMethod),
            new Claim(ParkDataEditorAuthenticationDefaults.TokenIdClaim, tokenId),
        }, "test"));
        return context;
    }
}
