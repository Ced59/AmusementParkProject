using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Core.Domain.Images;
using AmusementPark.WebAPI.Contracts.Parks.Logos;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur de compatibilité ParkLogos reposant désormais sur la feature Images centralisée.
/// </summary>
[ApiController]
[Route("parks/{parkId}/logos")]
public sealed class ParkLogosController : ControllerBase
{
    private readonly ICommandHandler<LinkImageCommand, ApplicationResult<Image>> linkImageCommandHandler;
    private readonly ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>> setCurrentImageCommandHandler;
    private readonly ICommandHandler<DeleteImageCommand, ApplicationResult> deleteImageCommandHandler;
    private readonly IQueryHandler<GetCurrentImageQuery, ApplicationResult<Image>> getCurrentImageQueryHandler;
    private readonly IQueryHandler<GetImagesByOwnerQuery, ApplicationResult<IReadOnlyCollection<Image>>> getImagesByOwnerQueryHandler;

    public ParkLogosController(
        ICommandHandler<LinkImageCommand, ApplicationResult<Image>> linkImageCommandHandler,
        ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>> setCurrentImageCommandHandler,
        ICommandHandler<DeleteImageCommand, ApplicationResult> deleteImageCommandHandler,
        IQueryHandler<GetCurrentImageQuery, ApplicationResult<Image>> getCurrentImageQueryHandler,
        IQueryHandler<GetImagesByOwnerQuery, ApplicationResult<IReadOnlyCollection<Image>>> getImagesByOwnerQueryHandler)
    {
        this.linkImageCommandHandler = linkImageCommandHandler;
        this.setCurrentImageCommandHandler = setCurrentImageCommandHandler;
        this.deleteImageCommandHandler = deleteImageCommandHandler;
        this.getCurrentImageQueryHandler = getCurrentImageQueryHandler;
        this.getImagesByOwnerQueryHandler = getImagesByOwnerQueryHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ParkLogoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddLogoAsync([FromRoute] string parkId, [FromBody] ParkLogoCreateDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> result = await this.linkImageCommandHandler.HandleAsync(
            new LinkImageCommand(request.ImageId, ImageOwnerType.Park, parkId, request.Description, true),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToParkLogoHttp());
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(ParkLogoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentLogoAsync([FromRoute] string parkId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> result = await this.getCurrentImageQueryHandler.HandleAsync(
            new GetCurrentImageQuery(parkId, ImageOwnerType.Park, ImageCategory.ParkLogo),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToParkLogoHttp());
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ParkLogoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogosHistoryAsync([FromRoute] string parkId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<Image>> result = await this.getImagesByOwnerQueryHandler.HandleAsync(
            new GetImagesByOwnerQuery(parkId, ImageOwnerType.Park, ImageCategory.ParkLogo),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        List<ParkLogoDto> response = result.Value.Select(static value => value.ToParkLogoHttp()).ToList();
        return this.Ok(response);
    }

    [HttpPut("{logoId}/set-current")]
    [ProducesResponseType(typeof(ParkLogoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetCurrentLogoAsync([FromRoute] string parkId, [FromRoute] string logoId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Image> result = await this.setCurrentImageCommandHandler.HandleAsync(
            new SetCurrentImageCommand(logoId, ImageOwnerType.Park, parkId),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToParkLogoHttp());
    }

    [HttpDelete("{logoId}")]
    public async Task<IActionResult> DeleteLogoAsync([FromRoute] string parkId, [FromRoute] string logoId, CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.deleteImageCommandHandler.HandleAsync(new DeleteImageCommand(logoId), cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(true);
    }
}
