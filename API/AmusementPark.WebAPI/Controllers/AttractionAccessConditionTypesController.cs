using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Commands;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.AttractionAccessConditionTypes;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("attraction-access-condition-types")]
[RequireActivatedUnblockedUser]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
public sealed class AttractionAccessConditionTypesController : ControllerBase
{
    private readonly IQueryHandler<ListAttractionAccessConditionTypeDefinitionsQuery, ApplicationResult<IReadOnlyCollection<AttractionAccessConditionTypeDefinition>>> listQueryHandler;
    private readonly ICommandHandler<UpsertAttractionAccessConditionTypeDefinitionCommand, ApplicationResult<AttractionAccessConditionTypeDefinition>> upsertCommandHandler;

    public AttractionAccessConditionTypesController(
        IQueryHandler<ListAttractionAccessConditionTypeDefinitionsQuery, ApplicationResult<IReadOnlyCollection<AttractionAccessConditionTypeDefinition>>> listQueryHandler,
        ICommandHandler<UpsertAttractionAccessConditionTypeDefinitionCommand, ApplicationResult<AttractionAccessConditionTypeDefinition>> upsertCommandHandler)
    {
        this.listQueryHandler = listQueryHandler;
        this.upsertCommandHandler = upsertCommandHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<AttractionAccessConditionTypeDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<AttractionAccessConditionTypeDefinition>> result = await this.listQueryHandler.HandleAsync(
            new ListAttractionAccessConditionTypeDefinitionsQuery(includeInactive),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.Select(static value => value.ToHttp()).ToList());
    }

    [HttpPost]
    [AdminAudit("attraction-access-condition-type.upsert", "AttractionAccessConditionType")]
    [ProducesResponseType(typeof(AttractionAccessConditionTypeDefinitionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertAsync([FromBody] UpsertAttractionAccessConditionTypeDefinitionDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<AttractionAccessConditionTypeDefinition> result = await this.upsertCommandHandler.HandleAsync(
            new UpsertAttractionAccessConditionTypeDefinitionCommand(request.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
