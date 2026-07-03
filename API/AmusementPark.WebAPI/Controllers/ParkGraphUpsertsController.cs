using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Commands;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.ParkGraphUpserts;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using AmusementPark.WebAPI.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/park-graph-upserts")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class ParkGraphUpsertsController : ControllerBase
{
    private readonly ICommandHandler<PreviewParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>> previewHandler;
    private readonly ICommandHandler<ApplyParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>> applyHandler;
    private readonly ICommandHandler<PreviewBulkParkGraphUpsertCommand, ApplicationResult<BulkParkGraphUpsertResult>> bulkPreviewHandler;
    private readonly ICommandHandler<ApplyBulkParkGraphUpsertCommand, ApplicationResult<BulkParkGraphUpsertResult>> bulkApplyHandler;
    private readonly IQueryHandler<ListParkGraphUpsertHistoryQuery, IReadOnlyCollection<ParkGraphUpsertHistoryEntry>> historyHandler;
    private readonly IQueryHandler<ExportParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> exportHandler;
    private readonly IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> bulkExportHandler;

    public ParkGraphUpsertsController(
        ICommandHandler<PreviewParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>> previewHandler,
        ICommandHandler<ApplyParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>> applyHandler,
        ICommandHandler<PreviewBulkParkGraphUpsertCommand, ApplicationResult<BulkParkGraphUpsertResult>> bulkPreviewHandler,
        ICommandHandler<ApplyBulkParkGraphUpsertCommand, ApplicationResult<BulkParkGraphUpsertResult>> bulkApplyHandler,
        IQueryHandler<ListParkGraphUpsertHistoryQuery, IReadOnlyCollection<ParkGraphUpsertHistoryEntry>> historyHandler,
        IQueryHandler<ExportParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> exportHandler,
        IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> bulkExportHandler)
    {
        this.previewHandler = previewHandler;
        this.applyHandler = applyHandler;
        this.bulkPreviewHandler = bulkPreviewHandler;
        this.bulkApplyHandler = bulkApplyHandler;
        this.historyHandler = historyHandler;
        this.exportHandler = exportHandler;
        this.bulkExportHandler = bulkExportHandler;
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(List<ParkGraphUpsertHistoryEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistoryAsync([FromQuery] string? targetParkId = null, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ParkGraphUpsertHistoryEntry> entries = await this.historyHandler.HandleAsync(
            new ListParkGraphUpsertHistoryQuery(targetParkId, limit),
            cancellationToken);

        return this.Ok(entries.Select(static entry => entry.ToHttp()).ToList());
    }

    [HttpGet("parks/{parkId}/export")]
    [AdminAudit("park-graph-upsert.export", "Park")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportParkJsonAsync([FromRoute] string parkId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkGraphJsonExportResult> result = await this.exportHandler.HandleAsync(
            new ExportParkGraphJsonQuery(parkId),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPost("bulk/export")]
    [AdminAudit("park-graph-upsert.bulk-export", "Park", StaticTargetId = "bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportBulkParkJsonAsync([FromBody] ParkGraphBulkExportRequestDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkGraphJsonExportResult> result = await this.bulkExportHandler.HandleAsync(
            new ExportBulkParkGraphJsonQuery(request.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPost("preview")]
    [AdminAudit("park-graph-upsert.preview", "Park")]
    [ProducesResponseType(typeof(ParkGraphUpsertResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewAsync([FromBody] ParkGraphUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkGraphUpsertResult> result = await this.previewHandler.HandleAsync(
            new PreviewParkGraphUpsertCommand(request.ToApplication(), this.GetCurrentUserId()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("bulk/preview")]
    [AdminAudit("park-graph-upsert.bulk-preview", "Park", StaticTargetId = "bulk")]
    [ProducesResponseType(typeof(BulkParkGraphUpsertResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewBulkAsync([FromBody] BulkParkGraphUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<BulkParkGraphUpsertResult> result = await this.bulkPreviewHandler.HandleAsync(
            new PreviewBulkParkGraphUpsertCommand(request.ToApplication(), this.GetCurrentUserId()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("apply")]
    [AdminAudit("park-graph-upsert.apply", "Park")]
    [InvalidatesPublicCache(PublicCacheScope.Data, PublicCacheScope.Seo, EvictOutputCache = false)]
    [ProducesResponseType(typeof(ParkGraphUpsertResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyAsync([FromBody] ParkGraphUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkGraphUpsertResult> result = await this.applyHandler.HandleAsync(
            new ApplyParkGraphUpsertCommand(request.ToApplication(), this.GetCurrentUserId()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("bulk/apply")]
    [AdminAudit("park-graph-upsert.bulk-apply", "Park", StaticTargetId = "bulk")]
    [InvalidatesPublicCache(PublicCacheScope.Data, PublicCacheScope.Seo, EvictOutputCache = false)]
    [ProducesResponseType(typeof(BulkParkGraphUpsertResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyBulkAsync([FromBody] BulkParkGraphUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<BulkParkGraphUpsertResult> result = await this.bulkApplyHandler.HandleAsync(
            new ApplyBulkParkGraphUpsertCommand(request.ToApplication(), this.GetCurrentUserId()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    private string? GetCurrentUserId()
    {
        return this.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? this.User.FindFirstValue("sub")
            ?? this.User.FindFirstValue("id");
    }
}
