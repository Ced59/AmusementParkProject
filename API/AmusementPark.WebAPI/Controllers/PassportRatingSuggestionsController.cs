using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Passport;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/passport/rating-update-suggestions")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PassportRatingSuggestionsController : ControllerBase
{
    private readonly IQueryHandler<
        GetGlobalRatingSuggestionsQuery,
        ApplicationResult<GlobalRatingSuggestionsResult>> queryHandler;
    private readonly ICommandHandler<
        SetGlobalRatingSuggestionsEnabledCommand,
        ApplicationResult<GlobalRatingSuggestionPreferenceResult>> preferenceHandler;
    private readonly ICommandHandler<
        RecordGlobalRatingSuggestionInteractionCommand,
        ApplicationResult<GlobalRatingSuggestionPreferenceResult>> interactionHandler;
    private readonly ICommandHandler<
        PresentGlobalRatingSuggestionsCommand,
        ApplicationResult<GlobalRatingSuggestionPresentationResult>> presentationHandler;

    public PassportRatingSuggestionsController(
        IQueryHandler<
            GetGlobalRatingSuggestionsQuery,
            ApplicationResult<GlobalRatingSuggestionsResult>> queryHandler,
        ICommandHandler<
            SetGlobalRatingSuggestionsEnabledCommand,
            ApplicationResult<GlobalRatingSuggestionPreferenceResult>> preferenceHandler,
        ICommandHandler<
            RecordGlobalRatingSuggestionInteractionCommand,
            ApplicationResult<GlobalRatingSuggestionPreferenceResult>> interactionHandler,
        ICommandHandler<
            PresentGlobalRatingSuggestionsCommand,
            ApplicationResult<GlobalRatingSuggestionPresentationResult>> presentationHandler)
    {
        this.queryHandler = queryHandler;
        this.preferenceHandler = preferenceHandler;
        this.interactionHandler = interactionHandler;
        this.presentationHandler = presentationHandler;
    }

    [HttpPost("presentations")]
    [ProducesResponseType(typeof(GlobalRatingSuggestionPresentationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PresentAsync(
        [FromBody] PresentGlobalRatingSuggestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.UnauthorizedResult();
        }

        ApplicationResult<GlobalRatingSuggestionPresentationResult> result =
            await this.presentationHandler.HandleAsync(
                new PresentGlobalRatingSuggestionsCommand(
                    userId,
                    request.Targets.Select(static target =>
                        new GlobalRatingSuggestionTargetKey(
                            target.TargetType.ToDomain(),
                            target.TargetId)).ToArray()),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(GlobalRatingSuggestionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAsync(
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.UnauthorizedResult();
        }

        ApplicationResult<GlobalRatingSuggestionsResult> result =
            await this.queryHandler.HandleAsync(
                new GetGlobalRatingSuggestionsQuery(userId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPut("preference")]
    [ProducesResponseType(typeof(GlobalRatingSuggestionPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetPreferenceAsync(
        [FromBody] SetGlobalRatingSuggestionPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.UnauthorizedResult();
        }

        ApplicationResult<GlobalRatingSuggestionPreferenceResult> result =
            await this.preferenceHandler.HandleAsync(
                new SetGlobalRatingSuggestionsEnabledCommand(userId, request.IsEnabled),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("interactions")]
    [ProducesResponseType(typeof(GlobalRatingSuggestionPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordInteractionAsync(
        [FromBody] RecordGlobalRatingSuggestionInteractionRequest request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.UnauthorizedResult();
        }

        ApplicationResult<GlobalRatingSuggestionPreferenceResult> result =
            await this.interactionHandler.HandleAsync(
                new RecordGlobalRatingSuggestionInteractionCommand(
                    userId,
                    request.TargetType.ToDomain(),
                    request.TargetId,
                    request.InteractionType.ToDomain()),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    private IActionResult UnauthorizedResult()
    {
        return this.ToProblemDetailsResult(
            StatusCodes.Status401Unauthorized,
            "Authentication is required.",
            "auth.unauthorized");
    }
}
