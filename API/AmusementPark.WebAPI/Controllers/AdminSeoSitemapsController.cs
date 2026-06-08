using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Commands;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Seo;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.OutputCaching;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Pilotage admin des sitemaps avancés et d'IndexNow.
/// </summary>
[ApiController]
[Route("admin/seo/sitemaps")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class AdminSeoSitemapsController : ControllerBase
{
    private readonly SeoSettings settings;
    private readonly IWebHostEnvironment environment;
    private readonly IQueryHandler<GetSeoSitemapOverviewQuery, ApplicationResult<SeoSitemapOverviewResult>> overviewHandler;
    private readonly IQueryHandler<GetSeoSitemapSettingsQuery, ApplicationResult<SeoSitemapSettings>> settingsHandler;
    private readonly IQueryHandler<GetSeoSitemapHistoryQuery, ApplicationResult<PagedResult<SitemapGenerationHistoryEntry>>> historyHandler;
    private readonly ICommandHandler<UpdateSeoSitemapSettingsCommand, ApplicationResult<SeoSitemapSettings>> updateSettingsHandler;
    private readonly ICommandHandler<GenerateSitemapCommand, ApplicationResult<SitemapGenerationResult>> generateHandler;
    private readonly IOutputCacheStore outputCacheStore;

    public AdminSeoSitemapsController(
        IOptions<SeoSettings> settings,
        IWebHostEnvironment environment,
        IQueryHandler<GetSeoSitemapOverviewQuery, ApplicationResult<SeoSitemapOverviewResult>> overviewHandler,
        IQueryHandler<GetSeoSitemapSettingsQuery, ApplicationResult<SeoSitemapSettings>> settingsHandler,
        IQueryHandler<GetSeoSitemapHistoryQuery, ApplicationResult<PagedResult<SitemapGenerationHistoryEntry>>> historyHandler,
        ICommandHandler<UpdateSeoSitemapSettingsCommand, ApplicationResult<SeoSitemapSettings>> updateSettingsHandler,
        ICommandHandler<GenerateSitemapCommand, ApplicationResult<SitemapGenerationResult>> generateHandler,
        IOutputCacheStore outputCacheStore)
    {
        this.settings = settings.Value;
        this.environment = environment;
        this.overviewHandler = overviewHandler;
        this.settingsHandler = settingsHandler;
        this.historyHandler = historyHandler;
        this.updateSettingsHandler = updateSettingsHandler;
        this.generateHandler = generateHandler;
        this.outputCacheStore = outputCacheStore;
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(SeoSitemapOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        string publicBaseUrl = this.GetPublicBaseUrl();
        ApplicationResult<SeoSitemapOverviewResult> result = await this.overviewHandler.HandleAsync(
            new GetSeoSitemapOverviewQuery(publicBaseUrl),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp(publicBaseUrl));
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(SeoSitemapSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<SeoSitemapSettings> result = await this.settingsHandler.HandleAsync(new GetSeoSitemapSettingsQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("settings")]
    [AdminAudit("seo.sitemap.settings.update", "SeoSitemapSettings")]
    [ProducesResponseType(typeof(SeoSitemapSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettingsAsync([FromBody] UpdateSeoSitemapSettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<SeoSitemapSettings> result = await this.updateSettingsHandler.HandleAsync(
            new UpdateSeoSitemapSettingsCommand(
                request.IsIndexNowEnabled,
                request.SubmitToIndexNowAfterManualGeneration,
                request.SubmitToIndexNowAfterAutomaticGeneration,
                request.IndexNowKey,
                request.IndexNowKeyLocation,
                request.IndexNowEndpoints),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        await this.EvictSeoOutputCacheAsync(cancellationToken);
        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("generate")]
    [AdminAudit("seo.sitemap.generate", "SeoSitemap")]
    [ProducesResponseType(typeof(SeoSitemapGenerationResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateAsync([FromBody] GenerateSeoSitemapRequestDto? request, CancellationToken cancellationToken = default)
    {
        string publicBaseUrl = this.GetPublicBaseUrl();
        ApplicationResult<SitemapGenerationResult> result = await this.generateHandler.HandleAsync(
            new GenerateSitemapCommand(
                publicBaseUrl,
                this.settings.SupportedLanguages,
                SitemapGenerationTrigger.Manual,
                request?.SubmitToIndexNow ?? true,
                this.GetCurrentUserId(),
                this.GetCurrentUserEmail()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        await this.EvictSeoOutputCacheAsync(cancellationToken);
        return this.Ok(result.Value.ToHttp(publicBaseUrl));
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResponseDto<SeoSitemapGenerationHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistoryAsync([FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken cancellationToken = default)
    {
        string publicBaseUrl = this.GetPublicBaseUrl();
        ApplicationResult<PagedResult<SitemapGenerationHistoryEntry>> result = await this.historyHandler.HandleAsync(
            new GetSeoSitemapHistoryQuery(page, size),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(item => item.ToHttp(publicBaseUrl)));
    }

    private string GetPublicBaseUrl()
    {
        return this.settings.GetNormalizedPublicBaseUrl(requireHttps: !this.environment.IsDevelopment());
    }

    private async Task EvictSeoOutputCacheAsync(CancellationToken cancellationToken)
    {
        await this.outputCacheStore.EvictByTagAsync(ApiOutputCachePolicyNames.PublicSeoTag, cancellationToken);
    }

    private string? GetCurrentUserId()
    {
        return this.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? this.User.FindFirstValue("sub")
            ?? this.User.FindFirstValue("id");
    }

    private string? GetCurrentUserEmail()
    {
        return this.User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? this.User.FindFirstValue(ClaimTypes.Email)
            ?? this.User.FindFirstValue("email");
    }
}
