using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.ParkDataEditorOperations;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Security;
using AmusementPark.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("park-data-editor/operations")]
[Authorize(Policy = AuthorizationPolicyNames.ParkDataEditorToken)]
[AllowParkDataEditorToken]
[RequireActivatedUnblockedUser]
public sealed class ParkDataEditorOperationsController : ControllerBase
{
    private readonly IParkDataEditorOperationCoordinator coordinator;
    private readonly IBulkParkGraphExportJobService exportJobService;

    public ParkDataEditorOperationsController(
        IParkDataEditorOperationCoordinator coordinator,
        IBulkParkGraphExportJobService exportJobService)
    {
        this.coordinator = coordinator;
        this.exportJobService = exportJobService;
    }

    [HttpGet("status")]
    [SkipParkDataEditorOperationCoordination]
    [EnableRateLimiting(RateLimitPolicyNames.ParkDataEditorOperationStatus)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(ParkDataEditorOperationStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public IActionResult GetStatus()
    {
        string clientId = this.User.FindFirst(ParkDataEditorAuthenticationDefaults.TokenIdClaim)?.Value
            ?? "unknown-park-data-editor-token";
        ParkDataEditorOperationCoordinationSnapshot coordination = this.coordinator.GetSnapshot(clientId);
        IReadOnlyCollection<BulkParkGraphExportJobSnapshot> activeExports =
            this.exportJobService.GetActiveSnapshots();

        this.Response.Headers.CacheControl = "no-store, max-age=0";
        this.Response.Headers.Pragma = "no-cache";
        this.Response.Headers.Expires = "0";
        return this.Ok(new ParkDataEditorOperationStatusDto
        {
            ServerTimeUtc = coordination.ServerTimeUtc,
            IsBusy = coordination.IsBusy,
            HasActiveExport = coordination.HasActiveExport,
            CanStartResourceIntensiveOperation = coordination.CanStartResourceIntensiveOperation,
            ActiveRequestCount = coordination.ActiveRequestCount,
            ActiveExportCount = coordination.ActiveExportCount,
            MaxConcurrentRequests = coordination.MaxConcurrentRequests,
            MaxConcurrentResourceIntensiveOperations = coordination.MaxConcurrentResourceIntensiveOperations,
            RecommendedPollIntervalSeconds = coordination.RecommendedPollIntervalSeconds,
            RetryAfterSeconds = coordination.RetryAfterSeconds,
            ActiveRequests = coordination.ActiveRequests.Select(static request => new ParkDataEditorActiveRequestDto
            {
                OperationId = request.OperationId,
                Kind = request.Kind.ToString(),
                Method = request.Method,
                Path = request.Path,
                StartedAtUtc = request.StartedAtUtc,
                InitiatedByCurrentToken = request.InitiatedByCurrentClient,
            }).ToList(),
            ActiveExports = activeExports.Select(export => new ParkDataEditorActiveExportDto
            {
                JobId = export.JobId,
                Status = export.Status.ToString(),
                ProgressPercentage = export.ProgressPercentage,
                Message = export.Message,
                ExportedParkCount = export.ExportedParkCount,
                ProcessedParkCount = export.ProcessedParkCount,
                CreatedAtUtc = export.CreatedAtUtc,
                StartedAtUtc = export.StartedAtUtc,
                InitiatedByCurrentToken = string.Equals(
                    export.RequestedByClientId,
                    clientId,
                    StringComparison.Ordinal),
            }).ToList(),
        });
    }
}
