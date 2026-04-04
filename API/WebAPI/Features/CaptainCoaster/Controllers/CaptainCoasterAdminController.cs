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

        [HttpGet("/admin/data/sources")]
        public async Task<ActionResult<IReadOnlyCollection<AdminDataSourceSummaryResponse>>> GetSourcesAsync()
        {
            IReadOnlyCollection<AdminDataSourceSummaryResponse> response = await captainCoasterAdminService.GetSourcesAsync();
            return Ok(response);
        }

        [HttpGet("settings")]
        public async Task<ActionResult<CaptainCoasterSettingsResponse>> GetSettingsAsync()
        {
            CaptainCoasterSettingsResponse response = await captainCoasterAdminService.GetSettingsAsync();
            return Ok(response);
        }

        [HttpPut("settings")]
        public async Task<ActionResult<CaptainCoasterSettingsResponse>> UpdateSettingsAsync([FromBody] UpdateCaptainCoasterSettingsRequest request)
        {
            CaptainCoasterSettingsResponse response = await captainCoasterAdminService.UpdateSettingsAsync(request);
            return Ok(response);
        }

        [HttpGet("imports/latest")]
        public async Task<ActionResult<CaptainCoasterSyncSessionResponse>> GetLatestSessionAsync()
        {
            CaptainCoasterSyncSessionResponse? response = await captainCoasterAdminService.GetLatestSessionAsync();
            if (response == null)
            {
                return NoContent();
            }

            return Ok(response);
        }

        [HttpPost("import-json")]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
        [RequestSizeLimit(100_000_000)]
        public async Task<ActionResult<CaptainCoasterSyncSessionResponse>> ImportJsonAsync([FromForm] List<IFormFile> files, CancellationToken cancellationToken)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "Aucun fichier JSON n'a été fourni." });
            }

            List<CaptainCoasterImportedFile> importedFiles = new();
            foreach (IFormFile file in files)
            {
                using StreamReader reader = new(file.OpenReadStream());
                string content = await reader.ReadToEndAsync(cancellationToken);
                importedFiles.Add(new CaptainCoasterImportedFile
                {
                    FileName = file.FileName,
                    Content = content
                });
            }

            CaptainCoasterSyncSessionResponse response = await captainCoasterAdminService.ImportJsonAsync(importedFiles, cancellationToken);
            return Accepted(response);
        }

        [HttpGet("comparison-results")]
        public async Task<ActionResult<IReadOnlyCollection<CaptainCoasterComparisonResultResponse>>> GetComparisonResultsAsync([FromQuery] string? sessionId)
        {
            IReadOnlyCollection<CaptainCoasterComparisonResultResponse> response = await captainCoasterAdminService.GetComparisonResultsAsync(sessionId);
            return Ok(response);
        }

        [HttpPost("apply")]
        public async Task<ActionResult<object>> ApplyAsync([FromBody] ApplyCaptainCoasterComparisonRequest request, CancellationToken cancellationToken)
        {
            int appliedCount = await captainCoasterAdminService.ApplyComparisonResultsAsync(request, cancellationToken);
            return Ok(new { appliedCount });
        }
    }
}
