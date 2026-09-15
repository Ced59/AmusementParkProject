using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Watchlists;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/collections")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UserCollectionsController : ControllerBase
{
    private readonly IQueryHandler<ListMyUserCollectionEntriesQuery,
        ApplicationResult<IReadOnlyCollection<UserCollectionEntryResult>>> listHandler;
    private readonly ICommandHandler<AddUserCollectionEntryCommand,
        ApplicationResult<UserCollectionEntryResult>> addHandler;
    private readonly ICommandHandler<DeleteUserCollectionEntryCommand, ApplicationResult>
        deleteHandler;

    public UserCollectionsController(
        IQueryHandler<ListMyUserCollectionEntriesQuery,
            ApplicationResult<IReadOnlyCollection<UserCollectionEntryResult>>> listHandler,
        ICommandHandler<AddUserCollectionEntryCommand,
            ApplicationResult<UserCollectionEntryResult>> addHandler,
        ICommandHandler<DeleteUserCollectionEntryCommand, ApplicationResult> deleteHandler)
    {
        this.listHandler = listHandler ?? throw new ArgumentNullException(nameof(listHandler));
        this.addHandler = addHandler ?? throw new ArgumentNullException(nameof(addHandler));
        this.deleteHandler = deleteHandler ?? throw new ArgumentNullException(nameof(deleteHandler));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserCollectionEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] string? targetType = null,
        [FromQuery] string? targetId = null,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        CollectionTargetType? parsedTargetType = null;
        if (!string.IsNullOrWhiteSpace(targetType))
        {
            if (!Enum.TryParse(targetType, true, out CollectionTargetType value)
                || !Enum.IsDefined(value))
            {
                return this.BadRequest();
            }

            parsedTargetType = value;
        }

        if (!string.IsNullOrWhiteSpace(targetId) && !parsedTargetType.HasValue)
        {
            return this.BadRequest();
        }

        ApplicationResult<IReadOnlyCollection<UserCollectionEntryResult>> result =
            await this.listHandler.HandleAsync(
                new ListMyUserCollectionEntriesQuery(userId, parsedTargetType, targetId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.Select(static entry => entry.ToHttp()).ToArray())
            : this.ToActionResult(result);
    }

    [HttpPut("{targetType}/{targetId}/{kind}")]
    [ProducesResponseType(typeof(UserCollectionEntryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddAsync(
        [FromRoute] string targetType,
        [FromRoute] string targetId,
        [FromRoute] string kind,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!UserCollectionEntryHttpMapper.TryToApplication(
            targetType,
            targetId,
            kind,
            out UserCollectionTargetInput? input)
            || input is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<UserCollectionEntryResult> result = await this.addHandler.HandleAsync(
            new AddUserCollectionEntryCommand(userId, input),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete("{targetType}/{targetId}/{kind}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string targetType,
        [FromRoute] string targetId,
        [FromRoute] string kind,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!UserCollectionEntryHttpMapper.TryToApplication(
            targetType,
            targetId,
            kind,
            out UserCollectionTargetInput? input)
            || input is null)
        {
            return this.BadRequest();
        }

        ApplicationResult result = await this.deleteHandler.HandleAsync(
            new DeleteUserCollectionEntryCommand(userId, input),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
