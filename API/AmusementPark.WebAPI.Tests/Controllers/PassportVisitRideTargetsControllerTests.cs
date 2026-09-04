using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Passport;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class PassportVisitRideTargetsControllerTests
{
    [Fact]
    public async Task EvaluateAsync_ShouldForwardTheAuthenticatedVisitScopeAndMapEvidence()
    {
        Mock<IQueryHandler<
            EvaluateVisitRideTargetsQuery,
            ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>>>> handler =
                new Mock<IQueryHandler<
                    EvaluateVisitRideTargetsQuery,
                    ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>>>>(
                        MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<EvaluateVisitRideTargetsQuery>(query =>
                    query.UserId == "owner-1"
                    && query.VisitId == "visit-1"
                    && query.ParkItemIds.SequenceEqual(new[] { "item-1" })),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>>.Success(
                new[]
                {
                    new VisitRideTargetEvaluationResult(
                        "item-1",
                        HistoricalConsistency.ConfirmedConflict,
                        new DateOnly(2000, 1, 1),
                        new DateOnly(2010, 12, 31)),
                }));
        PassportVisitRideTargetsController controller =
            new PassportVisitRideTargetsController(handler.Object)
            {
                ControllerContext = CreateControllerContext(),
            };

        IActionResult result = await controller.EvaluateAsync(
            "visit-1",
            new EvaluatePassportVisitRideTargetsRequestDto
            {
                ParkItemIds = new[] { "item-1" },
            },
            CancellationToken.None);

        PassportVisitRideTargetEvaluationDto evaluation = Assert.Single(
            Assert.IsType<OkObjectResult>(result).Value as
                IReadOnlyCollection<PassportVisitRideTargetEvaluationDto>
                ?? Array.Empty<PassportVisitRideTargetEvaluationDto>());
        Assert.Equal(
            PassportHistoricalConsistencyDto.ConfirmedConflict,
            evaluation.HistoricalConsistency);
        Assert.Equal(new DateOnly(2000, 1, 1), evaluation.OpeningDate);
        handler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldRemainPrivateAndNonCacheable()
    {
        RouteAttribute? route = typeof(PassportVisitRideTargetsController)
            .GetCustomAttribute<RouteAttribute>();
        Assert.Equal("me/passport/visits/{visitId}/ride-targets", route?.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(PassportVisitRideTargetsController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(typeof(PassportVisitRideTargetsController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute? cache = typeof(PassportVisitRideTargetsController)
            .GetCustomAttribute<ResponseCacheAttribute>();
        Assert.True(cache?.NoStore);
        HttpPostAttribute? post = typeof(PassportVisitRideTargetsController)
            .GetMethod(nameof(PassportVisitRideTargetsController.EvaluateAsync))
            ?.GetCustomAttribute<HttpPostAttribute>();
        Assert.Equal(":evaluate", post?.Template);
    }

    private static ControllerContext CreateControllerContext()
    {
        ClaimsIdentity identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "owner-1") },
            "Test");
        DefaultHttpContext context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
        };
        return new ControllerContext { HttpContext = context };
    }
}
