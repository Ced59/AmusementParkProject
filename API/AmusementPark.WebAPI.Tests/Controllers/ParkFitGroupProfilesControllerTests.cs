using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.WebAPI.Contracts.ParkFit;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ParkFitGroupProfilesControllerTests
{
    [Fact]
    public async Task ListAsync_ShouldUseAuthenticatedOwnerAndExposeOpaqueProfileId()
    {
        ParkFitGroupProfileResult profile = BuildProfile();
        Mock<IQueryHandler<ListMyParkFitGroupProfilesQuery,
            ApplicationResult<IReadOnlyCollection<ParkFitGroupProfileResult>>>> handler =
            new Mock<IQueryHandler<ListMyParkFitGroupProfilesQuery,
                ApplicationResult<IReadOnlyCollection<ParkFitGroupProfileResult>>>>(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                new ListMyParkFitGroupProfilesQuery("user-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<IReadOnlyCollection<ParkFitGroupProfileResult>>
                .Success(new[] { profile }));
        ParkFitGroupProfilesController controller = CreateController(listHandler: handler.Object);

        IActionResult result = await controller.ListAsync(CancellationToken.None);

        ParkFitGroupProfileDto dto = Assert.Single(
            Assert.IsType<ParkFitGroupProfileDto[]>(
                Assert.IsType<OkObjectResult>(result).Value));
        Assert.Equal("opaque-profile", dto.ProfileId);
        Assert.DoesNotContain(
            dto.GetType().GetProperties(),
            static property => property.Name.Contains("Owner", StringComparison.Ordinal));
        handler.VerifyAll();
    }

    [Fact]
    public async Task ExportAsync_ShouldNeverExposeOwnerProfileIdOrVersion()
    {
        ParkFitGroupProfileExportResult export = new(
            new DateTime(2026, 9, 14, 14, 0, 0, DateTimeKind.Utc),
            new[]
            {
                new ParkFitGroupProfileExportItemResult("Alex", 170, 30, false, null),
            });
        Mock<IQueryHandler<ExportMyParkFitGroupProfilesQuery,
            ApplicationResult<ParkFitGroupProfileExportResult>>> handler = new(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                new ExportMyParkFitGroupProfilesQuery("user-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ParkFitGroupProfileExportResult>.Success(export));
        ParkFitGroupProfilesController controller = CreateController(exportHandler: handler.Object);

        IActionResult result = await controller.ExportAsync(CancellationToken.None);

        ParkFitGroupProfileExportDto dto = Assert.IsType<ParkFitGroupProfileExportDto>(
            Assert.IsType<OkObjectResult>(result).Value);
        ParkFitGroupProfileExportItemDto item = Assert.Single(dto.Profiles);
        Assert.Equal("Alex", item.Alias);
        Assert.DoesNotContain(
            item.GetType().GetProperties(),
            static property => property.Name.Contains("Id", StringComparison.Ordinal)
                || property.Name.Contains("Version", StringComparison.Ordinal));
        handler.VerifyAll();
    }

    private static ParkFitGroupProfilesController CreateController(
        IQueryHandler<ListMyParkFitGroupProfilesQuery,
            ApplicationResult<IReadOnlyCollection<ParkFitGroupProfileResult>>>?
            listHandler = null,
        IQueryHandler<ExportMyParkFitGroupProfilesQuery,
            ApplicationResult<ParkFitGroupProfileExportResult>>? exportHandler = null)
    {
        ParkFitGroupProfilesController controller = new(
            listHandler ?? Mock.Of<IQueryHandler<ListMyParkFitGroupProfilesQuery,
                ApplicationResult<IReadOnlyCollection<ParkFitGroupProfileResult>>>>(
                MockBehavior.Strict),
            exportHandler ?? Mock.Of<IQueryHandler<ExportMyParkFitGroupProfilesQuery,
                ApplicationResult<ParkFitGroupProfileExportResult>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<CreateParkFitGroupProfileCommand,
                ApplicationResult<ParkFitGroupProfileResult>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<UpdateParkFitGroupProfileCommand,
                ApplicationResult<ParkFitGroupProfileResult>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<DeleteParkFitGroupProfileCommand,
                ApplicationResult>>(MockBehavior.Strict));
        ClaimsIdentity identity = new(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
            "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            },
        };
        return controller;
    }

    private static ParkFitGroupProfileResult BuildProfile()
    {
        DateTime nowUtc = new DateTime(2026, 9, 14, 14, 0, 0, DateTimeKind.Utc);
        return new ParkFitGroupProfileResult(
            "opaque-profile",
            "Alex",
            170,
            30,
            false,
            null,
            nowUtc,
            nowUtc,
            1);
    }
}
