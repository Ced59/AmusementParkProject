using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Contracts.History;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[AllowAnonymous]
[Route("public/history/subjects")]
public sealed class PublicHistoricalLineageController : ControllerBase
{
    private readonly IQueryHandler<
        GetPublicHistoricalLineageQuery,
        ApplicationResult<PublicHistoricalLineageResult>> handler;

    public PublicHistoricalLineageController(
        IQueryHandler<
            GetPublicHistoricalLineageQuery,
            ApplicationResult<PublicHistoricalLineageResult>> handler)
    {
        this.handler = handler;
    }

    [HttpGet("{type}/{id}/lineage")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [ProducesResponseType(typeof(PublicHistoricalLineageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string type,
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse(type, true, out HistoricalSubjectType subjectType)
            || !Enum.IsDefined(subjectType))
        {
            return this.BadRequest(new { error = "history.subject_type.invalid" });
        }

        ApplicationResult<PublicHistoricalLineageResult> result = await this.handler.HandleAsync(
            new GetPublicHistoricalLineageQuery(subjectType, id),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
