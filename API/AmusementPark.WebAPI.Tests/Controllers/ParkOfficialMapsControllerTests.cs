using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ParkOfficialMapsControllerTests
{
    [Theory]
    [InlineData(false, false, "public,max-age=86400")]
    [InlineData(true, true, "private,no-store")]
    public async Task GetFileAsync_ShouldKeepAdminPreviewsOutOfSharedCaches(
        bool isAdmin,
        bool expectedIncludeHidden,
        string expectedCacheControl)
    {
        Mock<IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>>> handler =
            new Mock<IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>>>(MockBehavior.Strict);
        handler
            .Setup(candidate => candidate.HandleAsync(
                It.Is<GetParkOfficialMapFileQuery>(query => query.ParkId == "park-1"
                    && query.OfficialMapId == "map-2026"
                    && query.IncludeHidden == expectedIncludeHidden),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<ParkOfficialMapBinary>.Success(new ParkOfficialMapBinary
            {
                CopyToAsync = async (destination, _, _, cancellationToken) =>
                    await destination.WriteAsync(new byte[] { 1, 2, 3 }, cancellationToken),
                ContentType = "application/pdf",
                FileName = "map-2026.pdf",
                SizeInBytes = 3,
                DisplayInline = true,
            }));
        ParkOfficialMapsController controller = new ParkOfficialMapsController(handler.Object);
        ClaimsIdentity identity = isAdmin
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "ADMIN") }, "test")
            : new ClaimsIdentity();
        DefaultHttpContext httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
        };
        httpContext.Response.Body = new MemoryStream();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        IActionResult result = await controller.GetFileAsync("park-1", "map-2026", CancellationToken.None);

        Assert.IsType<EmptyResult>(result);
        Assert.Equal(expectedCacheControl, controller.Response.Headers.CacheControl);
        Assert.Equal("nosniff", controller.Response.Headers.XContentTypeOptions);
        Assert.Equal("bytes", controller.Response.Headers.AcceptRanges);
        Assert.Equal(3, controller.Response.ContentLength);
        Assert.Equal(new byte[] { 1, 2, 3 }, ((MemoryStream)controller.Response.Body).ToArray());
        handler.VerifyAll();
    }

    [Fact]
    public async Task GetFileAsync_WhenRequestIsHead_ShouldReturnMetadataWithoutReadingMinio()
    {
        int copyCount = 0;
        Mock<IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>>> handler =
            BuildSuccessfulHandler((_, _, _, _) =>
            {
                copyCount++;
                return Task.CompletedTask;
            });
        ParkOfficialMapsController controller = BuildController(handler.Object, HttpMethods.Head);

        IActionResult result = await controller.GetFileAsync("park-1", "map-2026", CancellationToken.None);

        Assert.IsType<EmptyResult>(result);
        Assert.Equal(0, copyCount);
        Assert.Equal("application/pdf", controller.Response.ContentType);
        Assert.Equal(6, controller.Response.ContentLength);
        handler.VerifyAll();
    }

    [Fact]
    public async Task GetFileAsync_WhenRequestHasRange_ShouldStreamOnlyRequestedBytes()
    {
        byte[] source = new byte[] { 1, 2, 3, 4, 5, 6 };
        Mock<IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>>> handler =
            BuildSuccessfulHandler(async (destination, offset, length, cancellationToken) =>
            {
                await destination.WriteAsync(
                    source.AsMemory((int)offset, (int)length!.Value),
                    cancellationToken);
            });
        ParkOfficialMapsController controller = BuildController(handler.Object, HttpMethods.Get);
        controller.Request.Headers.Range = "bytes=2-4";

        IActionResult result = await controller.GetFileAsync("park-1", "map-2026", CancellationToken.None);

        Assert.IsType<EmptyResult>(result);
        Assert.Equal(StatusCodes.Status206PartialContent, controller.Response.StatusCode);
        Assert.Equal("bytes 2-4/6", controller.Response.Headers.ContentRange);
        Assert.Equal(3, controller.Response.ContentLength);
        Assert.Equal(new byte[] { 3, 4, 5 }, ((MemoryStream)controller.Response.Body).ToArray());
        handler.VerifyAll();
    }

    [Fact]
    public async Task GetFileAsync_WhenRangeIsOutsideFile_ShouldReturnRangeNotSatisfiableWithoutReadingMinio()
    {
        int copyCount = 0;
        Mock<IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>>> handler =
            BuildSuccessfulHandler((_, _, _, _) =>
            {
                copyCount++;
                return Task.CompletedTask;
            });
        ParkOfficialMapsController controller = BuildController(handler.Object, HttpMethods.Get);
        controller.Request.Headers.Range = "bytes=20-30";

        IActionResult result = await controller.GetFileAsync("park-1", "map-2026", CancellationToken.None);

        Assert.IsType<EmptyResult>(result);
        Assert.Equal(StatusCodes.Status416RangeNotSatisfiable, controller.Response.StatusCode);
        Assert.Equal("bytes */6", controller.Response.Headers.ContentRange);
        Assert.Equal(0, copyCount);
        handler.VerifyAll();
    }

    private static Mock<IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>>> BuildSuccessfulHandler(
        Func<Stream, long, long?, CancellationToken, Task> copyToAsync)
    {
        Mock<IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>>> handler =
            new Mock<IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>>>(MockBehavior.Strict);
        handler
            .Setup(candidate => candidate.HandleAsync(
                It.Is<GetParkOfficialMapFileQuery>(query => query.ParkId == "park-1"
                    && query.OfficialMapId == "map-2026"
                    && !query.IncludeHidden),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<ParkOfficialMapBinary>.Success(new ParkOfficialMapBinary
            {
                CopyToAsync = copyToAsync,
                ContentType = "application/pdf",
                FileName = "map-2026.pdf",
                SizeInBytes = 6,
                DisplayInline = true,
            }));
        return handler;
    }

    private static ParkOfficialMapsController BuildController(
        IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>> handler,
        string method)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Response.Body = new MemoryStream();
        return new ParkOfficialMapsController(handler)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }
}
