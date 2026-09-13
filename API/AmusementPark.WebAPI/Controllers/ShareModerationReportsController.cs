using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("passport/shared/reports")]
public sealed class ShareModerationReportsController : ControllerBase
{
    private readonly ICommandHandler<SubmitShareModerationReportCommand, ApplicationResult> handler;

    public ShareModerationReportsController(
        ICommandHandler<SubmitShareModerationReportCommand, ApplicationResult> handler)
    {
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.ShareModerationReports)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> SubmitAsync(
        [FromBody] SubmitShareModerationReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!request.TryToCommand(out SubmitShareModerationReportCommand? command)
            || command is null)
        {
            return this.BadRequest();
        }

        ApplicationResult result = await this.handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? this.Accepted() : this.ToActionResult(result);
    }
}
