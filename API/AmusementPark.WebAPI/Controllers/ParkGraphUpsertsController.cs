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
using AmusementPark.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using AmusementPark.WebAPI.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/park-graph-upserts")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class ParkGraphUpsertsController : ControllerBase
{
    internal const string ForwardedPrefixHeaderName = "X-Forwarded-Prefix";

    private readonly ICommandHandler<PreviewParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>> previewHandler;
    private readonly ICommandHandler<ApplyParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>> applyHandler;
    private readonly ICommandHandler<PreviewBulkParkGraphUpsertCommand, ApplicationResult<BulkParkGraphUpsertResult>> bulkPreviewHandler;
    private readonly ICommandHandler<ApplyBulkParkGraphUpsertCommand, ApplicationResult<BulkParkGraphUpsertResult>> bulkApplyHandler;
    private readonly IQueryHandler<ListParkGraphUpsertHistoryQuery, IReadOnlyCollection<ParkGraphUpsertHistoryEntry>> historyHandler;
    private readonly IQueryHandler<ExportParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> exportHandler;
    private readonly IQueryHandler<ExportStandaloneAttractionGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> standaloneAttractionExportHandler;
    private readonly IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> bulkExportHandler;
    private readonly IBulkParkGraphExportJobService bulkExportJobService;

    public ParkGraphUpsertsController(
        ICommandHandler<PreviewParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>> previewHandler,
        ICommandHandler<ApplyParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>> applyHandler,
        ICommandHandler<PreviewBulkParkGraphUpsertCommand, ApplicationResult<BulkParkGraphUpsertResult>> bulkPreviewHandler,
        ICommandHandler<ApplyBulkParkGraphUpsertCommand, ApplicationResult<BulkParkGraphUpsertResult>> bulkApplyHandler,
        IQueryHandler<ListParkGraphUpsertHistoryQuery, IReadOnlyCollection<ParkGraphUpsertHistoryEntry>> historyHandler,
        IQueryHandler<ExportParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> exportHandler,
        IQueryHandler<ExportStandaloneAttractionGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> standaloneAttractionExportHandler,
        IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> bulkExportHandler,
        IBulkParkGraphExportJobService bulkExportJobService)
    {
        this.previewHandler = previewHandler;
        this.applyHandler = applyHandler;
        this.bulkPreviewHandler = bulkPreviewHandler;
        this.bulkApplyHandler = bulkApplyHandler;
        this.historyHandler = historyHandler;
        this.exportHandler = exportHandler;
        this.standaloneAttractionExportHandler = standaloneAttractionExportHandler;
        this.bulkExportHandler = bulkExportHandler;
        this.bulkExportJobService = bulkExportJobService;
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

    [HttpPost("bulk/export-jobs")]
    [AdminAudit("park-graph-upsert.bulk-export-job", "Park", StaticTargetId = "bulk")]
    [ProducesResponseType(typeof(ParkGraphBulkExportJobDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> StartBulkParkJsonExportJobAsync([FromBody] ParkGraphBulkExportRequestDto request, CancellationToken cancellationToken = default)
    {
        string? currentUserId = this.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return this.Unauthorized();
        }

        BulkParkGraphExportJobSnapshot snapshot = await this.bulkExportJobService.StartAsync(
            request.ToApplication(),
            currentUserId,
            cancellationToken);

        return this.Accepted(this.ToHttp(snapshot));
    }

    [HttpGet("bulk/export-jobs/{jobId}")]
    [ProducesResponseType(typeof(ParkGraphBulkExportJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetBulkParkJsonExportJobAsync([FromRoute] string jobId)
    {
        string? currentUserId = this.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return this.Unauthorized();
        }

        BulkParkGraphExportJobSnapshot? snapshot = this.bulkExportJobService.GetSnapshot(jobId, currentUserId);
        if (snapshot is null)
        {
            return this.NotFound();
        }

        return this.Ok(this.ToHttp(snapshot));
    }

    [HttpGet("bulk/export-jobs/{jobId}/download")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DownloadBulkParkJsonExportJobAsync([FromRoute] string jobId, [FromQuery] string token)
    {
        BulkParkGraphExportDownload? download = this.bulkExportJobService.GetDownload(jobId, token);
        if (download is null)
        {
            return this.NotFound();
        }

        this.Response.Headers.CacheControl = "no-store, max-age=0";
        this.Response.Headers.Pragma = "no-cache";
        this.Response.Headers.Expires = "0";
        this.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        return this.PhysicalFile(download.FilePath, download.ContentType, download.FileName);
    }

    [HttpGet("standalone-attractions/{standaloneAttractionId}/export")]
    [AdminAudit("park-graph-upsert.standalone-attraction.export", "StandaloneAttraction")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportStandaloneAttractionJsonAsync([FromRoute] string standaloneAttractionId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkGraphJsonExportResult> result = await this.standaloneAttractionExportHandler.HandleAsync(
            new ExportStandaloneAttractionGraphJsonQuery(standaloneAttractionId),
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

    private ParkGraphBulkExportJobDto ToHttp(BulkParkGraphExportJobSnapshot snapshot)
    {
        return new ParkGraphBulkExportJobDto
        {
            JobId = snapshot.JobId,
            Status = snapshot.Status.ToString(),
            ProgressPercentage = snapshot.ProgressPercentage,
            Message = snapshot.Message,
            ExportedParkCount = snapshot.ExportedParkCount,
            ProcessedParkCount = snapshot.ProcessedParkCount,
            FileName = snapshot.FileName,
            ContentLength = snapshot.ContentLength,
            DownloadUrl = this.BuildBulkExportDownloadUrl(snapshot),
            CreatedAtUtc = snapshot.CreatedAtUtc,
            StartedAtUtc = snapshot.StartedAtUtc,
            CompletedAtUtc = snapshot.CompletedAtUtc,
            ExpiresAtUtc = snapshot.ExpiresAtUtc,
            Error = snapshot.Error,
        };
    }

    internal static string BuildBulkExportDownloadUrl(HttpRequest request, string jobId, string token)
    {
        string pathPrefix = GetPublicPathPrefix(request);
        string escapedJobId = Uri.EscapeDataString(jobId);
        string escapedToken = Uri.EscapeDataString(token);
        return $"{request.Scheme}://{request.Host}{pathPrefix}/admin/park-graph-upserts/bulk/export-jobs/{escapedJobId}/download?token={escapedToken}";
    }

    private string? BuildBulkExportDownloadUrl(BulkParkGraphExportJobSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.DownloadToken))
        {
            return null;
        }

        return BuildBulkExportDownloadUrl(this.Request, snapshot.JobId, snapshot.DownloadToken);
    }

    private static string GetPublicPathPrefix(HttpRequest request)
    {
        if (request.Headers.TryGetValue(ForwardedPrefixHeaderName, out StringValues forwardedPrefixValues))
        {
            foreach (string? rawForwardedPrefix in forwardedPrefixValues)
            {
                if (string.IsNullOrWhiteSpace(rawForwardedPrefix))
                {
                    continue;
                }

                string[] candidates = rawForwardedPrefix.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (string candidate in candidates)
                {
                    string? normalizedPrefix = NormalizePublicPathPrefix(candidate);
                    if (normalizedPrefix is not null)
                    {
                        return normalizedPrefix;
                    }
                }
            }
        }

        if (!request.PathBase.HasValue)
        {
            return string.Empty;
        }

        return NormalizePublicPathPrefix(request.PathBase.Value ?? string.Empty) ?? string.Empty;
    }

    private static string? NormalizePublicPathPrefix(string value)
    {
        string trimmedValue = value.Trim().TrimEnd('/');
        if (trimmedValue.Length == 0 || string.Equals(trimmedValue, "/", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        if (!trimmedValue.StartsWith("/", StringComparison.Ordinal)
            || trimmedValue.StartsWith("//", StringComparison.Ordinal)
            || trimmedValue.Contains('\\', StringComparison.Ordinal)
            || trimmedValue.Contains(':', StringComparison.Ordinal)
            || trimmedValue.Contains('?', StringComparison.Ordinal)
            || trimmedValue.Contains('#', StringComparison.Ordinal))
        {
            return null;
        }

        return trimmedValue;
    }
}
