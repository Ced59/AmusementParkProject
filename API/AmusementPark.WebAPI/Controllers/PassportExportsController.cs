using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Passport;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/passport/exports")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PassportExportsController : ControllerBase
{
    private readonly ICommandHandler<RequestPassportExportCommand, ApplicationResult<PassportExport>> requestHandler;
    private readonly IQueryHandler<GetPassportExportQuery, ApplicationResult<PassportExport>> getHandler;
    private readonly IQueryHandler<DownloadPassportExportQuery, ApplicationResult<PassportExportDownload>> downloadHandler;

    public PassportExportsController(
        ICommandHandler<RequestPassportExportCommand, ApplicationResult<PassportExport>> requestHandler,
        IQueryHandler<GetPassportExportQuery, ApplicationResult<PassportExport>> getHandler,
        IQueryHandler<DownloadPassportExportQuery, ApplicationResult<PassportExportDownload>> downloadHandler)
    {
        this.requestHandler = requestHandler;
        this.getHandler = getHandler;
        this.downloadHandler = downloadHandler;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicyNames.PassportExports)]
    [ProducesResponseType(typeof(PassportExportDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestAsync(
        [FromBody] RequestPassportExportDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status401Unauthorized,
                "Authentication is required.",
                "auth.unauthorized");
        }

        ApplicationResult<PassportExport> result = await this.requestHandler.HandleAsync(
            request.ToApplication(userId),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        string basePath = this.BuildBasePath();
        string location = $"{basePath}/{Uri.EscapeDataString(result.Value.Id)}";
        this.Response.Headers.RetryAfter = "2";
        return this.Accepted(location, result.Value.ToHttp(basePath));
    }

    [HttpGet("{exportId}")]
    [ProducesResponseType(typeof(PassportExportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string exportId,
        [FromQuery] bool download = false,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status401Unauthorized,
                "Authentication is required.",
                "auth.unauthorized");
        }

        if (download)
        {
            return await this.DownloadAsync(userId, exportId, cancellationToken);
        }

        ApplicationResult<PassportExport> result = await this.getHandler.HandleAsync(
            new GetPassportExportQuery(userId, exportId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp(this.BuildBasePath()))
            : this.ToActionResult(result);
    }

    private async Task<IActionResult> DownloadAsync(
        string userId,
        string exportId,
        CancellationToken cancellationToken)
    {
        ApplicationResult<PassportExportDownload> result =
            await this.downloadHandler.HandleAsync(
                new DownloadPassportExportQuery(userId, exportId),
                cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        this.Response.Headers["X-Content-SHA256"] = result.Value.ChecksumSha256;
        return this.File(
            result.Value.Content,
            result.Value.ContentType,
            result.Value.FileName,
            enableRangeProcessing: false);
    }

    private string BuildBasePath()
    {
        return $"{this.Request.GetPublicPathPrefix()}/me/passport/exports";
    }
}
