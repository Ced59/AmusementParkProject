using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ProfileComparisonInvitationsControllerTests
{
    [Fact]
    public async Task CreateAsync_ShouldUseAuthenticatedUserAndReturnOpaqueInvitation()
    {
        Mock<ICommandHandler<CreateProfileComparisonInvitationCommand,
            ApplicationResult<ProfileComparisonInvitationCreationResult>>> handler = new(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<CreateProfileComparisonInvitationCommand>(command =>
                    command.UserId == "user-1"
                    && command.Categories.SequenceEqual(
                        new[] { ProfileComparisonCategory.VisitedParks })),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ProfileComparisonInvitationCreationResult>.Success(
                new ProfileComparisonInvitationCreationResult(
                    "opaque-token",
                    new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc),
                    new[] { ProfileComparisonCategory.VisitedParks })));
        ProfileComparisonInvitationsController controller = CreateController(
            createHandler: handler.Object);

        IActionResult result = await controller.CreateAsync(
            new CreateProfileComparisonInvitationRequestDto
            {
                Categories = new List<string> { "VisitedParks" },
            },
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        ProfileComparisonInvitationCreationDto response =
            Assert.IsType<ProfileComparisonInvitationCreationDto>(ok.Value);
        Assert.Equal("opaque-token", response.Token);
        Assert.Equal(new[] { "VisitedParks" }, response.Categories);
        handler.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_ShouldNotExposePassportOrUserIdentifiers()
    {
        Mock<IQueryHandler<GetProfileComparisonInvitationPreviewQuery,
            ApplicationResult<ProfileComparisonInvitationPreviewResult>>> handler = new(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<GetProfileComparisonInvitationPreviewQuery>(query =>
                    query.UserId == "user-1" && query.Token == "opaque-token"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ProfileComparisonInvitationPreviewResult>.Success(
                new ProfileComparisonInvitationPreviewResult(
                    ProfileComparisonInvitationPreviewStatus.Ready,
                    "Camille",
                    "Alex",
                    new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc),
                    null,
                    new[] { ProfileComparisonCategory.VisitedParks },
                    true)));
        ProfileComparisonInvitationsController controller = CreateController(
            previewHandler: handler.Object);

        IActionResult result = await controller.PreviewAsync(
            "opaque-token",
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        ProfileComparisonInvitationPreviewDto response =
            Assert.IsType<ProfileComparisonInvitationPreviewDto>(ok.Value);
        Assert.Equal("Camille", response.CreatorDisplayName);
        Assert.Equal("Ready", response.Status);
        Assert.DoesNotContain(
            response.GetType().GetProperties(),
            static property => property.Name.EndsWith("Id", StringComparison.Ordinal));
        handler.VerifyAll();
    }

    [Fact]
    public async Task AcceptAsync_ShouldPreserveComparisonIdAsOpaqueShareTokenAlias()
    {
        DateTime acceptedAtUtc = new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);
        Mock<ICommandHandler<AcceptProfileComparisonInvitationCommand,
            ApplicationResult<ProfileComparisonInvitationAcceptanceResult>>> handler = new(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<AcceptProfileComparisonInvitationCommand>(command =>
                    command.UserId == "user-1" && command.Token == "opaque-token"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ProfileComparisonInvitationAcceptanceResult>.Success(
                new ProfileComparisonInvitationAcceptanceResult(
                    "opaque-share-token",
                    acceptedAtUtc,
                    new[] { ProfileComparisonCategory.VisitedParks })));
        ProfileComparisonInvitationsController controller = CreateController(
            acceptHandler: handler.Object);

        IActionResult result = await controller.AcceptAsync(
            "opaque-token",
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        ProfileComparisonInvitationAcceptanceDto response =
            Assert.IsType<ProfileComparisonInvitationAcceptanceDto>(ok.Value);
        Assert.Equal("opaque-share-token", response.ShareId);
        Assert.Equal(response.ShareId, response.ComparisonId);
        Assert.Equal(acceptedAtUtc, response.AcceptedAtUtc);
        handler.VerifyAll();
    }

    [Theory]
    [InlineData(nameof(ProfileComparisonInvitationsController.CreateAsync),
        RateLimitPolicyNames.SharePublicationConfirmations)]
    [InlineData(nameof(ProfileComparisonInvitationsController.PreviewAsync),
        RateLimitPolicyNames.SharePublicationPreviews)]
    [InlineData(nameof(ProfileComparisonInvitationsController.AcceptAsync),
        RateLimitPolicyNames.SharePublicationConfirmations)]
    public void Endpoint_ShouldDisableCachingAndApplyTargetedRateLimit(
        string actionName,
        string expectedPolicy)
    {
        MethodInfo action = typeof(ProfileComparisonInvitationsController).GetMethod(actionName)
            ?? throw new InvalidOperationException("Action not found.");
        EnableRateLimitingAttribute rateLimit = Assert.IsType<EnableRateLimitingAttribute>(
            action.GetCustomAttribute<EnableRateLimitingAttribute>());

        Assert.Equal(expectedPolicy, rateLimit.PolicyName);
        Assert.NotNull(typeof(ProfileComparisonInvitationsController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(ProfileComparisonInvitationsController)
                .GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
    }

    private static ProfileComparisonInvitationsController CreateController(
        ICommandHandler<CreateProfileComparisonInvitationCommand,
            ApplicationResult<ProfileComparisonInvitationCreationResult>>? createHandler = null,
        IQueryHandler<GetProfileComparisonInvitationPreviewQuery,
            ApplicationResult<ProfileComparisonInvitationPreviewResult>>? previewHandler = null,
        ICommandHandler<AcceptProfileComparisonInvitationCommand,
            ApplicationResult<ProfileComparisonInvitationAcceptanceResult>>? acceptHandler = null)
    {
        ProfileComparisonInvitationsController controller =
            new ProfileComparisonInvitationsController(
                createHandler ?? Mock.Of<ICommandHandler<CreateProfileComparisonInvitationCommand,
                    ApplicationResult<ProfileComparisonInvitationCreationResult>>>(MockBehavior.Strict),
                previewHandler ?? Mock.Of<IQueryHandler<GetProfileComparisonInvitationPreviewQuery,
                    ApplicationResult<ProfileComparisonInvitationPreviewResult>>>(MockBehavior.Strict),
                acceptHandler ?? Mock.Of<ICommandHandler<AcceptProfileComparisonInvitationCommand,
                    ApplicationResult<ProfileComparisonInvitationAcceptanceResult>>>(MockBehavior.Strict),
                Options.Create(new SharePublicationRolloutSettings { Enabled = true }));
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
