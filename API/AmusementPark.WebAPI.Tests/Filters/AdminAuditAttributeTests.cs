using AmusementPark.Application.Features.AdminAudit.Models;
using AmusementPark.Application.Features.AdminAudit.Ports;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Filters;

public sealed class AdminAuditAttributeTests
{
    [Fact]
    public async Task OnActionExecutionAsync_WhenRequestIsCancelledAfterSuccess_ShouldCompleteAudit()
    {
        CancellationToken observedCancellationToken = new CancellationToken(canceled: true);
        Mock<IAdminAuditLogWriter> writer = new Mock<IAdminAuditLogWriter>(MockBehavior.Strict);
        writer.Setup(value => value.WriteAsync(
                It.IsAny<AdminAuditLogEntry>(),
                It.IsAny<CancellationToken>()))
            .Callback<AdminAuditLogEntry, CancellationToken>(
                (_, cancellationToken) => observedCancellationToken = cancellationToken)
            .Returns(Task.CompletedTask);
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(writer.Object);
        services.AddLogging();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
        };
        using CancellationTokenSource requestCancellation = new CancellationTokenSource();
        requestCancellation.Cancel();
        httpContext.RequestAborted = requestCancellation.Token;
        ActionContext actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        ActionExecutingContext executingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
        AdminAuditAttribute attribute = new AdminAuditAttribute("live.control.update", "LiveControl");

        await attribute.OnActionExecutionAsync(
            executingContext,
            () => Task.FromResult(new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object())
            {
                Result = new OkResult(),
            }));

        Assert.False(observedCancellationToken.CanBeCanceled);
        writer.VerifyAll();
    }
}
