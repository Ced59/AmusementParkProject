using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/profile-comparisons")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
public sealed class ProfileComparisonsController : ControllerBase
{
    private readonly IQueryHandler<ListMyProfileComparisonsQuery,
        ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>>> listHandler;
    private readonly ICommandHandler<RevokeProfileComparisonCommand,
        ApplicationResult<ProfileComparisonRevocationResult>> revokeHandler;

    public ProfileComparisonsController(
        IQueryHandler<ListMyProfileComparisonsQuery,
            ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>>> listHandler,
        ICommandHandler<RevokeProfileComparisonCommand,
            ApplicationResult<ProfileComparisonRevocationResult>> revokeHandler)
    {
        this.listHandler = listHandler ?? throw new ArgumentNullException(nameof(listHandler));
        this.revokeHandler = revokeHandler ?? throw new ArgumentNullException(nameof(revokeHandler));
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProfileComparisonSummaryDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>> result =
            await this.listHandler.HandleAsync(
                new ListMyProfileComparisonsQuery(userId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.Select(static comparison => comparison.ToHttp()).ToArray())
            : this.ToActionResult(result);
    }

    [HttpDelete("{shareId}")]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(ProfileComparisonRevocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAsync(
        [FromRoute] string shareId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<ProfileComparisonRevocationResult> result =
            await this.revokeHandler.HandleAsync(
                new RevokeProfileComparisonCommand(userId, shareId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
