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
                Content = new MemoryStream(new byte[] { 1, 2, 3 }),
                ContentType = "application/pdf",
                FileName = "map-2026.pdf",
                SizeInBytes = 3,
                DisplayInline = true,
            }));
        ParkOfficialMapsController controller = new ParkOfficialMapsController(handler.Object);
        ClaimsIdentity identity = isAdmin
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "ADMIN") }, "test")
            : new ClaimsIdentity();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            },
        };

        IActionResult result = await controller.GetFileAsync("park-1", "map-2026", CancellationToken.None);

        FileStreamResult file = Assert.IsType<FileStreamResult>(result);
        Assert.True(file.EnableRangeProcessing);
        Assert.Equal(expectedCacheControl, controller.Response.Headers.CacheControl);
        Assert.Equal("nosniff", controller.Response.Headers.XContentTypeOptions);
        handler.VerifyAll();
    }
}
