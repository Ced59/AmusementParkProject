using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Commands;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ParkGraphUpsertsControllerTests
{
    [Fact]
    public async Task ExportStandaloneAttractionJsonAsync_ShouldReturnHandlerDocument()
    {
        byte[] content = "{\"documentType\":\"standaloneAttractionGraph\"}"u8.ToArray();
        Mock<IQueryHandler<ExportStandaloneAttractionGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>> handler =
            new Mock<IQueryHandler<ExportStandaloneAttractionGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>>(MockBehavior.Strict);
        handler
            .Setup(current => current.HandleAsync(
                It.Is<ExportStandaloneAttractionGraphJsonQuery>(query => query.StandaloneAttractionId == "standalone-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ParkGraphJsonExportResult>.Success(new ParkGraphJsonExportResult
            {
                FileName = "standalone-1.json",
                Content = content,
            }));
        ParkGraphUpsertsController controller = CreateController(
            Mock.Of<IBulkParkGraphExportJobService>(),
            handler.Object);

        IActionResult result = await controller.ExportStandaloneAttractionJsonAsync("standalone-1", CancellationToken.None);

        FileContentResult fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("standalone-1.json", fileResult.FileDownloadName);
        Assert.Equal("application/json", fileResult.ContentType);
        Assert.Equal(content, fileResult.FileContents);
        handler.VerifyAll();
    }

    [Fact]
    public void DownloadBulkParkJsonExportJobAsync_ShouldAllowAnonymousTokenDownloadAndDisableResponseCache()
    {
        MethodInfo method = typeof(ParkGraphUpsertsController).GetMethod(nameof(ParkGraphUpsertsController.DownloadBulkParkJsonExportJobAsync))
            ?? throw new InvalidOperationException("ParkGraphUpsertsController.DownloadBulkParkJsonExportJobAsync was not found.");

        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
        ResponseCacheAttribute responseCacheAttribute = method.GetCustomAttribute<ResponseCacheAttribute>()
            ?? throw new InvalidOperationException("ResponseCacheAttribute was not found.");

        Assert.True(responseCacheAttribute.NoStore);
        Assert.Equal(ResponseCacheLocation.None, responseCacheAttribute.Location);
    }

    [Fact]
    public void DownloadBulkParkJsonExportJobAsync_ShouldEnableRangeProcessing()
    {
        Mock<IBulkParkGraphExportJobService> jobService = new Mock<IBulkParkGraphExportJobService>(MockBehavior.Strict);
        jobService
            .Setup(service => service.GetDownload("job-1", "download-token"))
            .Returns(new BulkParkGraphExportDownload
            {
                FilePath = Path.GetFullPath("bulk-export.json"),
                FileName = "bulk-export.json",
                ContentType = "application/octet-stream",
            });
        ParkGraphUpsertsController controller = CreateController(jobService.Object);

        IActionResult result = controller.DownloadBulkParkJsonExportJobAsync("job-1", "download-token");

        PhysicalFileResult physicalFileResult = Assert.IsType<PhysicalFileResult>(result);
        Assert.True(physicalFileResult.EnableRangeProcessing);
        Assert.Equal("bulk-export.json", physicalFileResult.FileDownloadName);
        Assert.Equal("application/octet-stream", physicalFileResult.ContentType);
        jobService.VerifyAll();
    }

    [Fact]
    public void BuildBulkExportDownloadUrl_ShouldUseForwardedPrefixWhenProxyStripsApiPrefix()
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("amusement-parks.fun");
        httpContext.Request.Headers[ParkGraphUpsertsController.ForwardedPrefixHeaderName] = "/api";

        string result = ParkGraphUpsertsController.BuildBulkExportDownloadUrl(httpContext.Request, "job 1", "token+value");

        Assert.Equal("https://amusement-parks.fun/api/admin/park-graph-upserts/bulk/export-jobs/job%201/download?token=token%2Bvalue", result);
    }

    [Fact]
    public void BuildBulkExportDownloadUrl_ShouldFallbackToPathBaseWhenNoForwardedPrefixExists()
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:44391");
        httpContext.Request.PathBase = "/backend";

        string result = ParkGraphUpsertsController.BuildBulkExportDownloadUrl(httpContext.Request, "job-1", "token");

        Assert.Equal("https://localhost:44391/backend/admin/park-graph-upserts/bulk/export-jobs/job-1/download?token=token", result);
    }

    private static ParkGraphUpsertsController CreateController(
        IBulkParkGraphExportJobService jobService,
        IQueryHandler<ExportStandaloneAttractionGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>? standaloneAttractionExportHandler = null)
    {
        ParkGraphUpsertsController controller = new ParkGraphUpsertsController(
            Mock.Of<ICommandHandler<PreviewParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>>>(),
            Mock.Of<ICommandHandler<ApplyParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>>>(),
            Mock.Of<ICommandHandler<PreviewBulkParkGraphUpsertCommand, ApplicationResult<BulkParkGraphUpsertResult>>>(),
            Mock.Of<ICommandHandler<ApplyBulkParkGraphUpsertCommand, ApplicationResult<BulkParkGraphUpsertResult>>>(),
            Mock.Of<IQueryHandler<ListParkGraphUpsertHistoryQuery, IReadOnlyCollection<ParkGraphUpsertHistoryEntry>>>(),
            Mock.Of<IQueryHandler<ExportParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>>(),
            standaloneAttractionExportHandler ?? Mock.Of<IQueryHandler<ExportStandaloneAttractionGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>>(),
            Mock.Of<IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>>(),
            jobService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }
}
