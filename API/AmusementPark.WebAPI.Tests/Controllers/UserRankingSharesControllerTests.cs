using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Contracts.Ratings;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class UserRankingSharesControllerTests
{
    [Fact]
    public async Task SetMyShareVisibilityAsync_ShouldAlwaysUseTheAuthenticatedOwnerIdentifier()
    {
        DateTime publishedAtUtc = new DateTime(2026, 8, 20, 18, 0, 0, DateTimeKind.Utc);
        Mock<ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>>> handler =
            new Mock<ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>>>(MockBehavior.Strict);
        handler
            .Setup(candidate => candidate.HandleAsync(
                It.Is<SetSharePublicationVisibilityCommand>(command =>
                    command.UserId == "owner-1"
                    && command.PublicationType == SharePublicationType.PersonalRanking
                    && command.SourceId == null
                    && command.IsPublic),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SharePublicationSettingsResult>.Success(
                new SharePublicationSettingsResult(true, "share-id", publishedAtUtc)));
        UserRankingSharesController controller = CreateController(handler.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.SetMyShareVisibilityAsync(
            new UserRankingShareVisibilityDto { IsPublic = true });

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        UserRankingShareSettingsDto response = Assert.IsType<UserRankingShareSettingsDto>(ok.Value);
        Assert.True(response.IsPublic);
        Assert.Equal("share-id", response.ShareId);
        handler.VerifyAll();
    }

    [Fact]
    public async Task SetMyShareVisibilityAsync_WhenOwnerClaimIsMissing_ShouldReturnUnauthorized()
    {
        Mock<ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>>> handler =
            new Mock<ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>>>(MockBehavior.Strict);
        UserRankingSharesController controller = CreateController(handler.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        IActionResult result = await controller.SetMyShareVisibilityAsync(
            new UserRankingShareVisibilityDto { IsPublic = false });

        UnauthorizedResult unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        handler.VerifyNoOtherCalls();
    }

    [Fact]
    public void SharingEndpoints_ShouldKeepOwnerMutationProtectedAndPublicReadsAnonymous()
    {
        MethodInfo mutation = GetAction(nameof(UserRankingSharesController.SetMyShareVisibilityAsync));
        AuthorizeAttribute authorize = Assert.Single(
            mutation.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.NotNull(mutation.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.NotNull(mutation.GetCustomAttribute<InvalidatesPublicCacheAttribute>());
        Assert.False(string.IsNullOrWhiteSpace(authorize.Roles));

        MethodInfo publicProfile = GetAction(nameof(UserRankingSharesController.GetSharedProfileAsync));
        Assert.NotNull(publicProfile.GetCustomAttribute<AllowAnonymousAttribute>());
        ResponseCacheAttribute noStore = Assert.IsType<ResponseCacheAttribute>(
            publicProfile.GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(noStore.NoStore);
        Assert.Equal(ResponseCacheLocation.None, noStore.Location);
    }

    private static MethodInfo GetAction(string name)
    {
        return typeof(UserRankingSharesController).GetMethod(name)
            ?? throw new InvalidOperationException($"Action {name} was not found.");
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

    private static UserRankingSharesController CreateController(
        ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>> mutationHandler)
    {
        return new UserRankingSharesController(
            new Mock<IQueryHandler<GetSharePublicationSettingsQuery, ApplicationResult<SharePublicationSettingsResult>>>(MockBehavior.Strict).Object,
            mutationHandler,
            new Mock<IQueryHandler<GetSharedUserRankingProfileQuery, ApplicationResult<SharedUserRankingProfileResult>>>(MockBehavior.Strict).Object,
            new Mock<IQueryHandler<GetSharedUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>>>(MockBehavior.Strict).Object,
            new Mock<IQueryHandler<GetSharedUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>>(MockBehavior.Strict).Object,
            new Mock<IQueryHandler<GetSharedUserRankingPreviewQuery, ApplicationResult<UserRankingSharePreviewFileResult>>>(MockBehavior.Strict).Object);
    }
}
