using System.Text;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ContextualBlocks.Commands;
using AmusementPark.Application.Features.ContextualBlocks.Queries;
using AmusementPark.Application.Features.ContextualBlocks.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.ContextualBlocks;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/contextual-blocks")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class ContextualBlocksController : ControllerBase
{
    private readonly IQueryHandler<ExportContextualBlockJsonQuery, ApplicationResult<ContextualBlockJsonExportResult>> exportHandler;
    private readonly ICommandHandler<PreviewContextualBlockJsonCommand, ApplicationResult<ContextualBlockPreviewResult>> previewHandler;

    public ContextualBlocksController(
        IQueryHandler<ExportContextualBlockJsonQuery, ApplicationResult<ContextualBlockJsonExportResult>> exportHandler,
        ICommandHandler<PreviewContextualBlockJsonCommand, ApplicationResult<ContextualBlockPreviewResult>> previewHandler)
    {
        this.exportHandler = exportHandler;
        this.previewHandler = previewHandler;
    }

    [HttpGet("{blockType}/{entityId}/export")]
    [AdminAudit("contextual-block.export", "ContextualBlock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportBlockJsonAsync([FromRoute] string blockType, [FromRoute] string entityId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ContextualBlockJsonExportResult> result = await this.exportHandler.HandleAsync(
            new ExportContextualBlockJsonQuery(blockType, entityId),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        byte[] content = Encoding.UTF8.GetBytes(result.Value.Json);
        return this.File(content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPost("{blockType}/{entityId}/preview")]
    [AdminAudit("contextual-block.preview", "ContextualBlock")]
    [ProducesResponseType(typeof(ContextualBlockPreviewResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewBlockJsonAsync(
        [FromRoute] string blockType,
        [FromRoute] string entityId,
        [FromBody] ContextualBlockPreviewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ContextualBlockPreviewResult> result = await this.previewHandler.HandleAsync(
            new PreviewContextualBlockJsonCommand(blockType, entityId, request.Document),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
