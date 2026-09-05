using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Parks;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using AmusementPark.WebAPI.Security;
using AmusementPark.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("park-data-editor/official-map-files")]
[Authorize(Policy = AuthorizationPolicyNames.ParkDataEditorToken)]
[AllowParkDataEditorToken]
[RequireActivatedUnblockedUser]
public sealed class ParkDataEditorOfficialMapsController : ControllerBase
{
    private readonly ICommandHandler<UploadParkOfficialMapFileCommand, ApplicationResult<ParkOfficialMapStoredFile>> uploadHandler;

    public ParkDataEditorOfficialMapsController(
        ICommandHandler<UploadParkOfficialMapFileCommand, ApplicationResult<ParkOfficialMapStoredFile>> uploadHandler)
    {
        this.uploadHandler = uploadHandler;
    }

    [HttpPost]
    [ParkDataEditorOperation(ParkDataEditorOperationKind.ResourceIntensive)]
    [AdminAudit("park-data-editor.official-map-file.upload", "ParkOfficialMap")]
    [EnableRateLimiting(RateLimitPolicyNames.ImageUploadProcessing)]
    [RequestSizeLimit(UploadParkOfficialMapFileCommandHandler.MaximumFileSizeInBytes + (64 * 1024))]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ParkOfficialMapFileCreatedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadAsync(
        [FromForm] ParkOfficialMapFileCreateDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.File is null)
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status400BadRequest,
                "An official map file is required.",
                "park.official-map.file-required");
        }

        await using Stream content = request.File.OpenReadStream();
        FilePayload file = new FilePayload
        {
            FileName = request.File.FileName,
            ContentType = request.File.ContentType,
            Length = request.File.Length,
            Content = content,
        };
        ApplicationResult<ParkOfficialMapStoredFile> result = await this.uploadHandler.HandleAsync(
            new UploadParkOfficialMapFileCommand(new ParkOfficialMapFileUploadRequest(
                request.ParkId,
                request.OfficialMapId,
                file)),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new ParkOfficialMapFileCreatedDto
        {
            StorageKey = result.Value.StorageKey,
            OriginalFileName = result.Value.OriginalFileName,
            ContentType = result.Value.ContentType,
            SizeInBytes = result.Value.SizeInBytes,
            SuggestedFormat = result.Value.SuggestedFormat.ToString(),
        });
    }
}
