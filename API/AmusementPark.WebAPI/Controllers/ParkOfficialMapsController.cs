using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.WebAPI.AdminPublicView;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("parks/{parkId}/official-maps")]
public sealed class ParkOfficialMapsController : ControllerBase
{
    private readonly IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>> fileHandler;

    public ParkOfficialMapsController(
        IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>> fileHandler)
    {
        this.fileHandler = fileHandler;
    }

    [HttpGet("{officialMapId}/file")]
    [HttpHead("{officialMapId}/file")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFileAsync(
        [FromRoute] string parkId,
        [FromRoute] string officialMapId,
        CancellationToken cancellationToken = default)
    {
        bool includeHidden = this.HttpContext.UserCanSeeNonVisibleInPublicView();
        ApplicationResult<ParkOfficialMapBinary> result = await this.fileHandler.HandleAsync(
            new GetParkOfficialMapFileQuery(parkId, officialMapId, includeHidden),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        ParkOfficialMapBinary binary = result.Value;
        ContentDispositionHeaderValue disposition = new ContentDispositionHeaderValue(
            binary.DisplayInline ? "inline" : "attachment")
        {
            FileNameStar = binary.FileName,
        };
        this.Response.Headers.ContentDisposition = disposition.ToString();
        this.Response.Headers.CacheControl = includeHidden
            ? "private,no-store"
            : "public,max-age=86400";
        this.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        return this.File(binary.Content, binary.ContentType, enableRangeProcessing: true);
    }
}
