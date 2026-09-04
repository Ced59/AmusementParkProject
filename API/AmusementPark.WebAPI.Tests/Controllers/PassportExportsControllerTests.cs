using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Passport;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class PassportExportsControllerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RequestAsync_ShouldUseAuthenticatedOwnerAndReturnPollingLocation()
    {
        Mock<ICommandHandler<RequestPassportExportCommand, ApplicationResult<PassportExport>>> request =
            new Mock<ICommandHandler<RequestPassportExportCommand, ApplicationResult<PassportExport>>>(
                MockBehavior.Strict);
        request.Setup(handler => handler.HandleAsync(
                new RequestPassportExportCommand("owner-1", PassportExportFormat.Csv),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PassportExport>.Success(CreateExport(PassportExportStatus.Pending)));
        PassportExportsController controller = CreateController(requestHandler: request.Object);
        controller.ControllerContext = CreateControllerContext();
        controller.Request.Headers["X-Forwarded-Prefix"] = "/api";

        IActionResult result = await controller.RequestAsync(
            new RequestPassportExportDto(PassportExportFormatDto.Csv),
            CancellationToken.None);

        AcceptedResult accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal("/api/me/passport/exports/export-1", accepted.Location);
        PassportExportDto body = Assert.IsType<PassportExportDto>(accepted.Value);
        Assert.Equal("Csv", body.Format);
        Assert.Null(body.DownloadUrl);
        Assert.Null(typeof(PassportExportDto).GetProperty("UserId"));
        Assert.Equal("2", controller.Response.Headers.RetryAfter);
        request.VerifyAll();
    }

    [Fact]
    public async Task GetAsync_WhenDownloadIsRequested_ShouldReturnOnlyOwnedArtifact()
    {
        byte[] content = "passport"u8.ToArray();
        Mock<IQueryHandler<DownloadPassportExportQuery, ApplicationResult<PassportExportDownload>>> download =
            new Mock<IQueryHandler<DownloadPassportExportQuery, ApplicationResult<PassportExportDownload>>>(
                MockBehavior.Strict);
        download.Setup(handler => handler.HandleAsync(
                new DownloadPassportExportQuery("owner-1", "export-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PassportExportDownload>.Success(
                new PassportExportDownload(
                    "passport.json",
                    "application/json",
                    content,
                    "checksum")));
        PassportExportsController controller = CreateController(downloadHandler: download.Object);
        controller.ControllerContext = CreateControllerContext();

        IActionResult result = await controller.GetAsync(
            "export-1",
            download: true,
            CancellationToken.None);

        FileContentResult file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(content, file.FileContents);
        Assert.Equal("passport.json", file.FileDownloadName);
        Assert.Equal("application/json", file.ContentType);
        Assert.Equal("checksum", controller.Response.Headers["X-Content-SHA256"]);
        download.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldExposePrivateNoStoreRateLimitedRoutes()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(PassportExportsController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("me/passport/exports", route.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(PassportExportsController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(typeof(PassportExportsController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(PassportExportsController).GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);

        MethodInfo request = Assert.IsAssignableFrom<MethodInfo>(
            typeof(PassportExportsController).GetMethod(nameof(PassportExportsController.RequestAsync)));
        Assert.NotNull(request.GetCustomAttribute<HttpPostAttribute>());
        Assert.Equal(
            RateLimitPolicyNames.PassportExports,
            Assert.IsType<EnableRateLimitingAttribute>(
                request.GetCustomAttribute<EnableRateLimitingAttribute>()).PolicyName);
        MethodInfo get = Assert.IsAssignableFrom<MethodInfo>(
            typeof(PassportExportsController).GetMethod(nameof(PassportExportsController.GetAsync)));
        Assert.Equal(
            "{exportId}",
            Assert.IsType<HttpGetAttribute>(get.GetCustomAttribute<HttpGetAttribute>()).Template);
    }

    [Fact]
    public void ExportFormatContract_ShouldUseStableStringValues()
    {
        string json = JsonSerializer.Serialize(
            new RequestPassportExportDto(PassportExportFormatDto.Csv));

        Assert.Contains("\"Csv\"", json, StringComparison.Ordinal);
    }

    private static PassportExportsController CreateController(
        ICommandHandler<RequestPassportExportCommand, ApplicationResult<PassportExport>>? requestHandler = null,
        IQueryHandler<GetPassportExportQuery, ApplicationResult<PassportExport>>? getHandler = null,
        IQueryHandler<DownloadPassportExportQuery, ApplicationResult<PassportExportDownload>>? downloadHandler = null)
    {
        return new PassportExportsController(
            requestHandler ?? Mock.Of<ICommandHandler<RequestPassportExportCommand, ApplicationResult<PassportExport>>>(),
            getHandler ?? Mock.Of<IQueryHandler<GetPassportExportQuery, ApplicationResult<PassportExport>>>(),
            downloadHandler ?? Mock.Of<IQueryHandler<DownloadPassportExportQuery, ApplicationResult<PassportExportDownload>>>());
    }

    private static PassportExport CreateExport(PassportExportStatus status)
    {
        return new PassportExport(
            "export-1",
            "owner-1",
            PassportExportFormat.Csv,
            status,
            1,
            NowUtc,
            NowUtc,
            NowUtc.AddHours(1));
    }

    private static ControllerContext CreateControllerContext()
    {
        ClaimsIdentity identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "owner-1"),
                new Claim(ClaimTypes.Role, "USER"),
            },
            "Test");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            },
        };
    }
}
