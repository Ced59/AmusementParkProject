using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.WebAPI.Contracts.ParkFit;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Recherche publique de compatibilité sans sauvegarde des critères personnels.
/// </summary>
[ApiController]
[Route("public/park-fit")]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PublicParkFitController : ControllerBase
{
    private readonly IQueryHandler<
        SearchParksByFitQuery,
        ApplicationResult<ParkFitSearchResult>> searchHandler;
    private readonly ICommandHandler<SubmitParkFitSourceReportCommand, ApplicationResult>
        reportHandler;

    public PublicParkFitController(
        IQueryHandler<
            SearchParksByFitQuery,
            ApplicationResult<ParkFitSearchResult>> searchHandler,
        ICommandHandler<SubmitParkFitSourceReportCommand, ApplicationResult> reportHandler)
    {
        this.searchHandler = searchHandler;
        this.reportHandler = reportHandler;
    }

    [HttpPost("search")]
    [EnableRateLimiting(RateLimitPolicyNames.ParkFitSearch)]
    [ProducesResponseType(typeof(ParkFitSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SearchAsync(
        [FromBody] ParkFitSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkFitSearchResult> result = await this.searchHandler.HandleAsync(
            request.ToApplication(),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("reports")]
    [EnableRateLimiting(RateLimitPolicyNames.ParkFitReports)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ReportAsync(
        [FromBody] SubmitParkFitSourceReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!request.TryToCommand(out SubmitParkFitSourceReportCommand? command)
            || command is null)
        {
            return this.BadRequest();
        }

        ApplicationResult result = await this.reportHandler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? this.Accepted() : this.ToActionResult(result);
    }
}
