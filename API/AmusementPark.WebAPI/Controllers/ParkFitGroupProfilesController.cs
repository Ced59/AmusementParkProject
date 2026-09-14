using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.ParkFit;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/park-fit/group-profiles")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
public sealed class ParkFitGroupProfilesController : ControllerBase
{
    private readonly IQueryHandler<ListMyParkFitGroupProfilesQuery,
        ApplicationResult<IReadOnlyCollection<ParkFitGroupProfileResult>>> listHandler;
    private readonly IQueryHandler<ExportMyParkFitGroupProfilesQuery,
        ApplicationResult<ParkFitGroupProfileExportResult>> exportHandler;
    private readonly ICommandHandler<CreateParkFitGroupProfileCommand,
        ApplicationResult<ParkFitGroupProfileResult>> createHandler;
    private readonly ICommandHandler<UpdateParkFitGroupProfileCommand,
        ApplicationResult<ParkFitGroupProfileResult>> updateHandler;
    private readonly ICommandHandler<DeleteParkFitGroupProfileCommand,
        ApplicationResult> deleteHandler;

    public ParkFitGroupProfilesController(
        IQueryHandler<ListMyParkFitGroupProfilesQuery,
            ApplicationResult<IReadOnlyCollection<ParkFitGroupProfileResult>>> listHandler,
        IQueryHandler<ExportMyParkFitGroupProfilesQuery,
            ApplicationResult<ParkFitGroupProfileExportResult>> exportHandler,
        ICommandHandler<CreateParkFitGroupProfileCommand,
            ApplicationResult<ParkFitGroupProfileResult>> createHandler,
        ICommandHandler<UpdateParkFitGroupProfileCommand,
            ApplicationResult<ParkFitGroupProfileResult>> updateHandler,
        ICommandHandler<DeleteParkFitGroupProfileCommand, ApplicationResult> deleteHandler)
    {
        this.listHandler = listHandler ?? throw new ArgumentNullException(nameof(listHandler));
        this.exportHandler = exportHandler ?? throw new ArgumentNullException(nameof(exportHandler));
        this.createHandler = createHandler ?? throw new ArgumentNullException(nameof(createHandler));
        this.updateHandler = updateHandler ?? throw new ArgumentNullException(nameof(updateHandler));
        this.deleteHandler = deleteHandler ?? throw new ArgumentNullException(nameof(deleteHandler));
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(IReadOnlyCollection<ParkFitGroupProfileDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<IReadOnlyCollection<ParkFitGroupProfileResult>> result =
            await this.listHandler.HandleAsync(
                new ListMyParkFitGroupProfilesQuery(userId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.Select(static profile => profile.ToHttp()).ToArray())
            : this.ToActionResult(result);
    }

    [HttpGet("export")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(ParkFitGroupProfileExportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<ParkFitGroupProfileExportResult> result =
            await this.exportHandler.HandleAsync(
                new ExportMyParkFitGroupProfilesQuery(userId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(ParkFitGroupProfileDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateParkFitGroupProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<ParkFitGroupProfileResult> result =
            await this.createHandler.HandleAsync(
                new CreateParkFitGroupProfileCommand(userId, request.ToApplication()),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.StatusCode(StatusCodes.Status201Created, result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPut("{profileId}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(ParkFitGroupProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] string profileId,
        [FromBody] UpdateParkFitGroupProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<ParkFitGroupProfileResult> result =
            await this.updateHandler.HandleAsync(
                new UpdateParkFitGroupProfileCommand(
                    userId,
                    profileId,
                    request.ExpectedVersion,
                    request.ToApplication()),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete("{profileId}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string profileId,
        [FromQuery] long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult result = await this.deleteHandler.HandleAsync(
            new DeleteParkFitGroupProfileCommand(userId, profileId, expectedVersion),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
