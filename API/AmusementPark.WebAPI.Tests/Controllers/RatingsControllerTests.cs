using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Ratings;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class RatingsControllerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UpsertAsync_ShouldUseOnlyTheAuthenticatedUserAsOwner()
    {
        Mock<ICommandHandler<
            UpsertUserRatingCommand,
            ApplicationResult<UserRatingResult>>> handler = new(MockBehavior.Strict);
        handler.Setup(candidate => candidate.HandleAsync(
                It.Is<UpsertUserRatingCommand>(command =>
                    command.UserId == "owner-a"
                    && command.TargetType == RatingTargetType.ParkItem
                    && command.TargetId == "item-1"
                    && command.Value == 4.5d),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<UserRatingResult>.Success(CreateRating()));
        RatingsController controller = CreateController(upsertHandler: handler.Object);
        controller.ControllerContext = CreateControllerContext("owner-a");

        IActionResult result = await controller.UpsertAsync(
            new UserRatingUpsertDto
            {
                TargetType = "ParkItem",
                TargetId = "item-1",
                Value = 4.5d,
            },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Null(typeof(UserRatingUpsertDto).GetProperty("UserId"));
        handler.VerifyAll();
    }

    [Fact]
    public async Task DeleteMyRatingForTargetAsync_ShouldUseOnlyTheAuthenticatedUserAsOwner()
    {
        Mock<ICommandHandler<
            DeleteUserRatingCommand,
            ApplicationResult<RatingSummaryResult>>> handler = new(MockBehavior.Strict);
        handler.Setup(candidate => candidate.HandleAsync(
                new DeleteUserRatingCommand("owner-a", RatingTargetType.ParkItem, "item-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<RatingSummaryResult>.Success(CreateSummary()));
        RatingsController controller = CreateController(deleteHandler: handler.Object);
        controller.ControllerContext = CreateControllerContext("owner-a");

        IActionResult result = await controller.DeleteMyRatingForTargetAsync(
            "ParkItem",
            "item-1",
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        handler.VerifyAll();
    }

    [Fact]
    public void PrivateEndpoints_ShouldRemainAuthenticatedActivatedAndNonCacheable()
    {
        string[] methodNames =
        {
            nameof(RatingsController.GetMyRatingsAsync),
            nameof(RatingsController.GetMyParkRankingsAsync),
            nameof(RatingsController.GetMyParkItemRankingsAsync),
            nameof(RatingsController.GetMyRatingStatsAsync),
            nameof(RatingsController.GetMyRatingForTargetAsync),
            nameof(RatingsController.UpsertAsync),
            nameof(RatingsController.DeleteMyRatingForTargetAsync),
        };

        foreach (string methodName in methodNames)
        {
            MethodInfo method = typeof(RatingsController).GetMethod(methodName)!;
            AuthorizeAttribute authorize = Assert.Single(
                method.GetCustomAttributes<AuthorizeAttribute>(),
                static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
            Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
            Assert.NotNull(method.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
            ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
                method.GetCustomAttribute<ResponseCacheAttribute>());
            Assert.True(cache.NoStore);
            Assert.Equal(ResponseCacheLocation.None, cache.Location);
        }
    }

    private static RatingsController CreateController(
        ICommandHandler<
            UpsertUserRatingCommand,
            ApplicationResult<UserRatingResult>>? upsertHandler = null,
        ICommandHandler<
            DeleteUserRatingCommand,
            ApplicationResult<RatingSummaryResult>>? deleteHandler = null)
    {
        return new RatingsController(
            upsertHandler ?? Mock.Of<ICommandHandler<
                UpsertUserRatingCommand,
                ApplicationResult<UserRatingResult>>>(MockBehavior.Strict),
            deleteHandler ?? Mock.Of<ICommandHandler<
                DeleteUserRatingCommand,
                ApplicationResult<RatingSummaryResult>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<
                GetRatingSummaryQuery,
                ApplicationResult<RatingSummaryResult>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<
                GetUserRatingQuery,
                ApplicationResult<UserRatingResult?>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<
                ListUserRatingsQuery,
                ApplicationResult<PagedResult<UserRatingListItemResult>>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<
                GetUserRatingStatsQuery,
                ApplicationResult<UserRatingStatsResult>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<
                GetRatingRankingsQuery,
                ApplicationResult<PagedResult<ParkRatingRankingResult>>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<
                GetParkItemRatingRankingsQuery,
                ApplicationResult<PagedResult<ParkItemRatingRankingResult>>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<
                GetUserParkRatingRankingsQuery,
                ApplicationResult<PagedResult<UserParkRatingRankingResult>>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<
                GetUserParkItemRatingRankingsQuery,
                ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>>(MockBehavior.Strict));
    }

    private static ControllerContext CreateControllerContext(string userId)
    {
        ClaimsIdentity identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
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

    private static UserRatingResult CreateRating()
    {
        return new UserRatingResult(
            "rating-1",
            "owner-a",
            RatingTargetType.ParkItem,
            "item-1",
            "park-1",
            ParkItemCategory.Attraction,
            ParkItemType.RollerCoaster,
            4.5d,
            NowUtc,
            NowUtc,
            CreateSummary());
    }

    private static RatingSummaryResult CreateSummary()
    {
        return new RatingSummaryResult(
            RatingTargetType.ParkItem,
            "item-1",
            1,
            4.5d,
            4.5d);
    }
}
