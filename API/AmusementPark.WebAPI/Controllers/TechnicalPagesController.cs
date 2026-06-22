using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.TechnicalPages.Commands;
using AmusementPark.Application.Features.TechnicalPages.Queries;
using AmusementPark.Application.Features.TechnicalPages.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.TechnicalPages;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("technical-pages")]
[RequireActivatedUnblockedUser]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[InvalidatesPublicCache(PublicCacheScope.ReferenceData, PublicCacheScope.Data, PublicCacheScope.Seo)]
public sealed class TechnicalPagesController : ControllerBase
{
    private readonly IQueryHandler<GetTechnicalPagesQuery, ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>> getTechnicalPagesQueryHandler;
    private readonly IQueryHandler<GetTechnicalPageLinkIndexQuery, ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>> getTechnicalPageLinkIndexQueryHandler;
    private readonly IQueryHandler<GetTechnicalPageByIdQuery, ApplicationResult<TechnicalPageResult>> getTechnicalPageByIdQueryHandler;
    private readonly IQueryHandler<GetTechnicalPageBySlugQuery, ApplicationResult<TechnicalPageResult>> getTechnicalPageBySlugQueryHandler;
    private readonly ICommandHandler<CreateTechnicalPageCommand, ApplicationResult<TechnicalPageResult>> createTechnicalPageCommandHandler;
    private readonly ICommandHandler<UpdateTechnicalPageCommand, ApplicationResult<TechnicalPageResult>> updateTechnicalPageCommandHandler;
    private readonly ICommandHandler<UpsertTechnicalPagesJsonCommand, ApplicationResult<TechnicalPageJsonUpsertResult>> upsertTechnicalPagesJsonCommandHandler;

    public TechnicalPagesController(
        IQueryHandler<GetTechnicalPagesQuery, ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>> getTechnicalPagesQueryHandler,
        IQueryHandler<GetTechnicalPageLinkIndexQuery, ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>> getTechnicalPageLinkIndexQueryHandler,
        IQueryHandler<GetTechnicalPageByIdQuery, ApplicationResult<TechnicalPageResult>> getTechnicalPageByIdQueryHandler,
        IQueryHandler<GetTechnicalPageBySlugQuery, ApplicationResult<TechnicalPageResult>> getTechnicalPageBySlugQueryHandler,
        ICommandHandler<CreateTechnicalPageCommand, ApplicationResult<TechnicalPageResult>> createTechnicalPageCommandHandler,
        ICommandHandler<UpdateTechnicalPageCommand, ApplicationResult<TechnicalPageResult>> updateTechnicalPageCommandHandler,
        ICommandHandler<UpsertTechnicalPagesJsonCommand, ApplicationResult<TechnicalPageJsonUpsertResult>> upsertTechnicalPagesJsonCommandHandler)
    {
        this.getTechnicalPagesQueryHandler = getTechnicalPagesQueryHandler;
        this.getTechnicalPageLinkIndexQueryHandler = getTechnicalPageLinkIndexQueryHandler;
        this.getTechnicalPageByIdQueryHandler = getTechnicalPageByIdQueryHandler;
        this.getTechnicalPageBySlugQueryHandler = getTechnicalPageBySlugQueryHandler;
        this.createTechnicalPageCommandHandler = createTechnicalPageCommandHandler;
        this.updateTechnicalPageCommandHandler = updateTechnicalPageCommandHandler;
        this.upsertTechnicalPagesJsonCommandHandler = upsertTechnicalPagesJsonCommandHandler;
    }

    [HttpGet]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicReferenceData)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<TechnicalPageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicAsync([FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<TechnicalPageResult>> result = await this.getTechnicalPagesQueryHandler.HandleAsync(new GetTechnicalPagesQuery(false), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<TechnicalPageDto> response = pagination.ToPagedResponse(result.Value, static value => value.ToHttp());
        return this.Ok(response);
    }

    [HttpGet("link-index")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicReferenceData)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TechnicalPageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicLinkIndexAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<TechnicalPageResult>> result = await this.getTechnicalPageLinkIndexQueryHandler.HandleAsync(new GetTechnicalPageLinkIndexQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        List<TechnicalPageDto> response = result.Value.Select(static value => value.ToHttp()).ToList();
        return this.Ok(response);
    }

    [HttpGet("admin")]
    [ProducesResponseType(typeof(PagedResponseDto<TechnicalPageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminAsync([FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<TechnicalPageResult>> result = await this.getTechnicalPagesQueryHandler.HandleAsync(new GetTechnicalPagesQuery(true), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<TechnicalPageDto> response = pagination.ToPagedResponse(result.Value, static value => value.ToHttp());
        return this.Ok(response);
    }

    [HttpGet("by-id/{id}")]
    [ProducesResponseType(typeof(TechnicalPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult<TechnicalPageResult> result = await this.getTechnicalPageByIdQueryHandler.HandleAsync(new GetTechnicalPageByIdQuery(id), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("slug/{slug}")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicReferenceData)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TechnicalPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySlugAsync([FromRoute] string slug, CancellationToken cancellationToken = default)
    {
        ApplicationResult<TechnicalPageResult> result = await this.getTechnicalPageBySlugQueryHandler.HandleAsync(new GetTechnicalPageBySlugQuery(slug, false), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost]
    [AdminAudit("technical-page.create", "TechnicalPage")]
    [ProducesResponseType(typeof(TechnicalPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromBody] TechnicalPageDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<TechnicalPageResult> result = await this.createTechnicalPageCommandHandler.HandleAsync(new CreateTechnicalPageCommand(dto.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{id}")]
    [AdminAudit("technical-page.update", "TechnicalPage", TargetIdRouteKey = "id")]
    [ProducesResponseType(typeof(TechnicalPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync([FromRoute] string id, [FromBody] TechnicalPageDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<TechnicalPageResult> result = await this.updateTechnicalPageCommandHandler.HandleAsync(new UpdateTechnicalPageCommand(id, dto.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("upsert-json")]
    [AdminAudit("technical-page.upsert-json", "TechnicalPage", StaticTargetId = "bulk")]
    [ProducesResponseType(typeof(TechnicalPagesJsonUpsertResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertJsonAsync([FromBody] TechnicalPagesJsonUpsertDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<TechnicalPageJsonUpsertResult> result = await this.upsertTechnicalPagesJsonCommandHandler.HandleAsync(
            new UpsertTechnicalPagesJsonCommand(dto.Pages.Select(page => page.ToDomain()).ToList()),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
