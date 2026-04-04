using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Features.CaptainCoaster.Contracts;
using WebAPI.Features.CaptainCoaster.Services;
using WebAPI.Settings.Attributes;

namespace WebAPI.Features.CaptainCoaster.Controllers
{
    [ApiController]
    [Route("admin/data/captain-coaster")]
    [Authorize(Roles = "ADMIN")]
    [RequireActivatedUnblockedUser]
    public sealed class CaptainCoasterAdminController : ControllerBase
    {
        private readonly ICaptainCoasterAdminService captainCoasterAdminService;

        public CaptainCoasterAdminController(ICaptainCoasterAdminService captainCoasterAdminService)
        {
            this.captainCoasterAdminService = captainCoasterAdminService;
        }

        [HttpGet("status")]
        public async Task<ActionResult<CaptainCoasterDataSourceStatusResponse>> GetStatusAsync()
        {
            CaptainCoasterDataSourceStatusResponse response = await captainCoasterAdminService.GetStatusAsync();
            return Ok(response);
        }

        [HttpGet("sessions/latest")]
        public async Task<ActionResult<CaptainCoasterSyncSessionResponse>> GetLatestSessionAsync()
        {
            CaptainCoasterSyncSessionResponse? response = await captainCoasterAdminService.GetLatestSessionAsync();
            if (response == null) { return NoContent(); }
            return Ok(response);
        }

        [HttpPost("import")]
        [RequestSizeLimit(200 * 1024 * 1024)]
        public async Task<ActionResult<CaptainCoasterSyncSessionResponse>> ImportFromFilesAsync(
            IFormFile parksFile,
            IFormFile coastersFile,
            CancellationToken cancellationToken)
        {
            if (parksFile == null || parksFile.Length == 0)
            {
                return BadRequest(new { message = "Le fichier detected-parks.json est requis." });
            }
            if (coastersFile == null || coastersFile.Length == 0)
            {
                return BadRequest(new { message = "Le fichier coasters.json est requis." });
            }

            await using Stream parksStream = parksFile.OpenReadStream();
            await using Stream coastersStream = coastersFile.OpenReadStream();

            CaptainCoasterSyncSessionResponse response = await captainCoasterAdminService.StartImportFromFilesAsync(
                parksStream, coastersStream, cancellationToken);

            return Accepted(response);
        }

        [HttpGet("comparison-results")]
        public async Task<ActionResult<CaptainCoasterComparisonPagedResponse>> GetComparisonResultsAsync(
            [FromQuery] string? sessionId,
            [FromQuery] string? entityType,
            [FromQuery] string? changeType,
            [FromQuery] bool? isApplied,
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 50)
        {
            CaptainCoasterComparisonPagedResponse response = await captainCoasterAdminService.GetComparisonResultsAsync(
                sessionId, entityType, changeType, isApplied, page, pageSize);
            return Ok(response);
        }

        [HttpPost("apply")]
        public async Task<ActionResult<ApplyCaptainCoasterComparisonResponse>> ApplyAsync(
            [FromBody] ApplyCaptainCoasterComparisonRequest request,
            CancellationToken cancellationToken)
        {
            int appliedCount = await captainCoasterAdminService.ApplyComparisonResultsAsync(request, cancellationToken);
            return Ok(new ApplyCaptainCoasterComparisonResponse { AppliedCount = appliedCount });
        }
    }
}
