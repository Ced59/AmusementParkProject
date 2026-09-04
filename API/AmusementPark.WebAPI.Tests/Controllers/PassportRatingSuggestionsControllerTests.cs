using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Ratings;
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

public sealed class PassportRatingSuggestionsControllerTests
{
    [Fact]
    public async Task GetAsync_UsesAuthenticatedOwnerAndMapsTheExplainedSuggestion()
    {
        HandlerMocks handlers = CreateHandlers();
        handlers.Query.Setup(value => value.HandleAsync(
                new GetGlobalRatingSuggestionsQuery("owner-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<GlobalRatingSuggestionsResult>.Success(
                new GlobalRatingSuggestionsResult(
                    true,
                    true,
                    2,
                    30,
                    new[]
                    {
                        new GlobalRatingSuggestionResult(
                            RatingTargetType.Park,
                            "park-1",
                            "Parc test",
                            "park-1",
                            "Parc test",
                            null,
                            4.5d,
                            3d,
                            3.25d,
                            4d,
                            2,
                            2,
                            GlobalRatingSuggestionReason.RecentExperiencesLower,
                            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc)),
                    })));
        PassportRatingSuggestionsController controller =
            CreateController(handlers, authenticated: true);

        IActionResult response = await controller.GetAsync();

        GlobalRatingSuggestionsDto body = Assert.IsType<GlobalRatingSuggestionsDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        GlobalRatingSuggestionDto suggestion = Assert.Single(body.Suggestions);
        Assert.Equal(GlobalRatingSuggestionTargetTypeDto.Park, suggestion.TargetType);
        Assert.Equal(GlobalRatingSuggestionReasonDto.RecentExperiencesLower, suggestion.Reason);
        Assert.Null(typeof(GlobalRatingSuggestionDto).GetProperty("UserId"));
        handlers.Query.VerifyAll();
    }

    [Fact]
    public async Task RecordInteractionAsync_MapsExplicitActionWithoutRatingValue()
    {
        HandlerMocks handlers = CreateHandlers();
        handlers.Interaction.Setup(value => value.HandleAsync(
                It.Is<RecordGlobalRatingSuggestionInteractionCommand>(command =>
                    command.UserId == "owner-1"
                    && command.TargetType == RatingTargetType.ParkItem
                    && command.TargetId == "item-1"
                    && command.InteractionType ==
                        AmusementPark.Application.Features.Passport.Models.GlobalRatingSuggestionInteractionType.Dismissed),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<GlobalRatingSuggestionPreferenceResult>.Success(
                new GlobalRatingSuggestionPreferenceResult(true, true)));
        PassportRatingSuggestionsController controller =
            CreateController(handlers, authenticated: true);

        IActionResult response = await controller.RecordInteractionAsync(
            new RecordGlobalRatingSuggestionInteractionRequest
            {
                TargetType = GlobalRatingSuggestionTargetTypeDto.ParkItem,
                TargetId = "item-1",
                InteractionType = GlobalRatingSuggestionInteractionTypeDto.Dismissed,
            });

        Assert.IsType<GlobalRatingSuggestionPreferenceDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Null(typeof(RecordGlobalRatingSuggestionInteractionRequest)
            .GetProperty("RatingValue"));
        handlers.Interaction.VerifyAll();
    }

    [Fact]
    public void Controller_IsPrivateNoStoreAndKeepsTheRoadmapGetRoute()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(PassportRatingSuggestionsController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("me/passport/rating-update-suggestions", route.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(PassportRatingSuggestionsController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(typeof(PassportRatingSuggestionsController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.True(Assert.IsType<ResponseCacheAttribute>(
            typeof(PassportRatingSuggestionsController)
                .GetCustomAttribute<ResponseCacheAttribute>()).NoStore);
        Assert.NotNull(typeof(PassportRatingSuggestionsController)
            .GetMethod(nameof(PassportRatingSuggestionsController.GetAsync))
            ?.GetCustomAttribute<HttpGetAttribute>());
    }

    private static HandlerMocks CreateHandlers()
    {
        return new HandlerMocks(
            new Mock<IQueryHandler<
                GetGlobalRatingSuggestionsQuery,
                ApplicationResult<GlobalRatingSuggestionsResult>>>(MockBehavior.Strict),
            new Mock<ICommandHandler<
                SetGlobalRatingSuggestionsEnabledCommand,
                ApplicationResult<GlobalRatingSuggestionPreferenceResult>>>(MockBehavior.Strict),
            new Mock<ICommandHandler<
                RecordGlobalRatingSuggestionInteractionCommand,
                ApplicationResult<GlobalRatingSuggestionPreferenceResult>>>(MockBehavior.Strict));
    }

    private static PassportRatingSuggestionsController CreateController(
        HandlerMocks handlers,
        bool authenticated)
    {
        ClaimsIdentity identity = authenticated
            ? new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "owner-1") },
                "Test")
            : new ClaimsIdentity();
        return new PassportRatingSuggestionsController(
            handlers.Query.Object,
            handlers.Preference.Object,
            handlers.Interaction.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity),
                },
            },
        };
    }

    private sealed record HandlerMocks(
        Mock<IQueryHandler<
            GetGlobalRatingSuggestionsQuery,
            ApplicationResult<GlobalRatingSuggestionsResult>>> Query,
        Mock<ICommandHandler<
            SetGlobalRatingSuggestionsEnabledCommand,
            ApplicationResult<GlobalRatingSuggestionPreferenceResult>>> Preference,
        Mock<ICommandHandler<
            RecordGlobalRatingSuggestionInteractionCommand,
            ApplicationResult<GlobalRatingSuggestionPreferenceResult>>> Interaction);
}
