using System.Security.Claims;
using AmusementPark.WebAPI.Contracts.ParkDataEditorOperations;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Security;
using AmusementPark.WebAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ParkDataEditorOperationsControllerTests
{
    [Fact]
    public void GetStatus_ShouldExposeAnExportStartedByAnotherTokenWithoutExposingItsIdentity()
    {
        ParkDataEditorOperationCoordinator coordinator = new ParkDataEditorOperationCoordinator();
        using ParkDataEditorOperationLease exportLease = coordinator.TryBeginExport("job-1", "token-a")!;
        Mock<IBulkParkGraphExportJobService> exportJobService =
            new Mock<IBulkParkGraphExportJobService>(MockBehavior.Strict);
        exportJobService
            .Setup(service => service.GetActiveSnapshots())
            .Returns(new[]
            {
                new BulkParkGraphExportJobSnapshot
                {
                    JobId = "job-1",
                    Status = BulkParkGraphExportJobStatus.Running,
                    ProgressPercentage = 40,
                    RequestedByClientId = "token-a",
                },
            });
        ParkDataEditorOperationsController controller = new ParkDataEditorOperationsController(
            coordinator,
            exportJobService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ParkDataEditorAuthenticationDefaults.TokenIdClaim, "token-b"),
                }, "test")),
            },
        };

        IActionResult result = controller.GetStatus();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        ParkDataEditorOperationStatusDto response = Assert.IsType<ParkDataEditorOperationStatusDto>(ok.Value);
        Assert.True(response.IsBusy);
        Assert.True(response.HasActiveExport);
        ParkDataEditorActiveExportDto activeExport = Assert.Single(response.ActiveExports);
        Assert.Equal("job-1", activeExport.JobId);
        Assert.False(activeExport.InitiatedByCurrentToken);
        Assert.False(response.CanStartResourceIntensiveOperation);
        exportJobService.VerifyAll();
    }
}
