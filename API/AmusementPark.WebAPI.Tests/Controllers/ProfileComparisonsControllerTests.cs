using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ProfileComparisonsControllerTests
{
    [Fact]
    public async Task ListAsync_ShouldUseAuthenticatedUserAndExposeNoInternalIdentifier()
    {
        Mock<IQueryHandler<ListMyProfileComparisonsQuery,
            ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>>>> handler =
            new Mock<IQueryHandler<ListMyProfileComparisonsQuery,
                ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>>>>(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                new ListMyProfileComparisonsQuery("user-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>>
                .Success(new[]
                {
                    new ProfileComparisonSummaryResult(
                        "opaque-share",
                        "Alex",
                        new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc),
                        new[] { ProfileComparisonCategory.VisitedParks },
                        false),
                }));
        ProfileComparisonsController controller = CreateController(listHandler: handler.Object);

        IActionResult result = await controller.ListAsync(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        ProfileComparisonSummaryDto response = Assert.Single(
            Assert.IsType<ProfileComparisonSummaryDto[]>(ok.Value));
        Assert.Equal("opaque-share", response.ShareId);
        Assert.DoesNotContain(
            response.GetType().GetProperties(),
            static property => property.Name.Contains("UserId", StringComparison.Ordinal)
                || property.Name.Contains("PublicationId", StringComparison.Ordinal));
        handler.VerifyAll();
    }

    [Fact]
    public async Task RevokeAsync_ShouldUseAuthenticatedParticipantAndOpaqueShareId()
    {
        Mock<ICommandHandler<RevokeProfileComparisonCommand,
            ApplicationResult<ProfileComparisonRevocationResult>>> handler = new(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                new RevokeProfileComparisonCommand("user-1", "opaque-share"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ProfileComparisonRevocationResult>.Success(
                new ProfileComparisonRevocationResult(
                    new DateTime(2026, 9, 13, 12, 5, 0, DateTimeKind.Utc))));
        ProfileComparisonsController controller = CreateController(revokeHandler: handler.Object);

        IActionResult result = await controller.RevokeAsync(
            "opaque-share",
            CancellationToken.None);

        ProfileComparisonRevocationDto response = Assert.IsType<ProfileComparisonRevocationDto>(
            Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(new DateTime(2026, 9, 13, 12, 5, 0, DateTimeKind.Utc),
            response.RevokedAtUtc);
        handler.VerifyAll();
    }

    private static ProfileComparisonsController CreateController(
        IQueryHandler<ListMyProfileComparisonsQuery,
            ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>>>?
            listHandler = null,
        ICommandHandler<RevokeProfileComparisonCommand,
            ApplicationResult<ProfileComparisonRevocationResult>>? revokeHandler = null)
    {
        ProfileComparisonsController controller = new ProfileComparisonsController(
            listHandler ?? Mock.Of<IQueryHandler<ListMyProfileComparisonsQuery,
                ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>>>>(
                MockBehavior.Strict),
            revokeHandler ?? Mock.Of<ICommandHandler<RevokeProfileComparisonCommand,
                ApplicationResult<ProfileComparisonRevocationResult>>>(MockBehavior.Strict));
        ClaimsIdentity identity = new ClaimsIdentity(
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
}
