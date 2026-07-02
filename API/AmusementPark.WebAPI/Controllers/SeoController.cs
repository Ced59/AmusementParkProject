using System.Text;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Responses;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Expose les fichiers techniques SEO publics.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class SeoController : ControllerBase
{
    private readonly SeoSettings settings;
    private readonly IWebHostEnvironment environment;
    private readonly ISeoSitemapSettingsRepository sitemapSettingsRepository;
    private readonly IQueryHandler<GetPublicSitemapDocumentQuery, ApplicationResult<SitemapDocumentResult>> getPublicSitemapDocumentQueryHandler;
    private readonly IQueryHandler<GetPublicHtmlSitemapNodesQuery, ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>> getPublicHtmlSitemapNodesQueryHandler;

    public SeoController(
        IOptions<SeoSettings> settings,
        IWebHostEnvironment environment,
        ISeoSitemapSettingsRepository sitemapSettingsRepository,
        IQueryHandler<GetPublicSitemapDocumentQuery, ApplicationResult<SitemapDocumentResult>> getPublicSitemapDocumentQueryHandler,
        IQueryHandler<GetPublicHtmlSitemapNodesQuery, ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>> getPublicHtmlSitemapNodesQueryHandler)
    {
        this.settings = settings.Value;
        this.environment = environment;
        this.sitemapSettingsRepository = sitemapSettingsRepository;
        this.getPublicSitemapDocumentQueryHandler = getPublicSitemapDocumentQueryHandler;
        this.getPublicHtmlSitemapNodesQueryHandler = getPublicHtmlSitemapNodesQueryHandler;
    }

    [HttpGet("/robots.txt")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicSeoDocuments)]
    [Produces("text/plain")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult GetRobotsTxt()
    {
        string publicBaseUrl = this.GetPublicBaseUrl();
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("User-agent: *");

        foreach (string allowPath in this.BuildRobotsAllowPaths())
        {
            builder.Append("Allow: ").AppendLine(allowPath);
        }

        foreach (string disallowPath in this.BuildRobotsDisallowPaths())
        {
            builder.Append("Disallow: ").AppendLine(disallowPath);
        }

        builder.AppendLine();
        builder.Append("Sitemap: ").Append(publicBaseUrl).AppendLine("/sitemap.xml");

        return this.Content(builder.ToString(), "text/plain", Encoding.UTF8);
    }

    [HttpHead("/robots.txt")]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HeadRobotsTxt()
    {
        this.Response.ContentType = "text/plain; charset=utf-8";
        return new EmptyResult();
    }

    [HttpGet("/sitemap.xml")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicSeoDocuments)]
    [Produces("application/xml")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSitemapIndexXml(CancellationToken cancellationToken = default)
    {
        return await this.GetSitemapDocumentAsync(sectionKey: null, cancellationToken);
    }

    [HttpHead("/sitemap.xml")]
    [Produces("application/xml")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HeadSitemapIndexXml(CancellationToken cancellationToken = default)
    {
        return await this.GetSitemapDocumentHeadAsync(sectionKey: null, cancellationToken);
    }

    [HttpGet("/sitemaps/{sectionFileName}")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicSeoDocuments)]
    [Produces("application/xml")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSitemapSectionXml([FromRoute] string sectionFileName, CancellationToken cancellationToken = default)
    {
        return await this.GetSitemapDocumentAsync(sectionFileName, cancellationToken);
    }

    [HttpHead("/sitemaps/{sectionFileName}")]
    [Produces("application/xml")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HeadSitemapSectionXml([FromRoute] string sectionFileName, CancellationToken cancellationToken = default)
    {
        return await this.GetSitemapDocumentHeadAsync(sectionFileName, cancellationToken);
    }

    [HttpGet("/seo/html-sitemap/nodes")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicHtmlSitemapNodes)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PublicHtmlSitemapNode>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicHtmlSitemapNodesAsync(
        [FromQuery] string language,
        [FromQuery] string? parentNodeId,
        [FromQuery] bool includeDescendants = false,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>> result = await this.getPublicHtmlSitemapNodesQueryHandler.HandleAsync(
            new GetPublicHtmlSitemapNodesQuery(language, parentNodeId, this.settings.SupportedLanguages, includeDescendants),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value);
    }

    [HttpGet("/{key}.txt")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicSeoDocuments)]
    [Produces("text/plain")]
    public async Task<IActionResult> GetIndexNowKeyFileAsync([FromRoute] string key, CancellationToken cancellationToken = default)
    {
        SeoSitemapSettings sitemapSettings = await this.sitemapSettingsRepository.GetAsync(cancellationToken);
        if (!IsIndexNowKeyFileRequest($"/{key}.txt", sitemapSettings))
        {
            return this.NotFound();
        }

        return this.Content(sitemapSettings.IndexNowKey, "text/plain", Encoding.UTF8);
    }

    [HttpHead("/{key}.txt")]
    [Produces("text/plain")]
    public async Task<IActionResult> HeadIndexNowKeyFileAsync([FromRoute] string key, CancellationToken cancellationToken = default)
    {
        SeoSitemapSettings sitemapSettings = await this.sitemapSettingsRepository.GetAsync(cancellationToken);
        if (!IsIndexNowKeyFileRequest($"/{key}.txt", sitemapSettings))
        {
            return this.NotFound();
        }

        this.Response.ContentType = "text/plain; charset=utf-8";
        return new EmptyResult();
    }

    private async Task<IActionResult> GetSitemapDocumentAsync(string? sectionKey, CancellationToken cancellationToken)
    {
        ApplicationResult<SitemapDocumentResult> result = await this.getPublicSitemapDocumentQueryHandler.HandleAsync(
            new GetPublicSitemapDocumentQuery(sectionKey, this.GetPublicBaseUrl(), this.settings.SupportedLanguages),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Content(result.Value.Content, result.Value.ContentType, Encoding.UTF8);
    }

    private async Task<IActionResult> GetSitemapDocumentHeadAsync(string? sectionKey, CancellationToken cancellationToken)
    {
        ApplicationResult<SitemapDocumentResult> result = await this.getPublicSitemapDocumentQueryHandler.HandleAsync(
            new GetPublicSitemapDocumentQuery(sectionKey, this.GetPublicBaseUrl(), this.settings.SupportedLanguages),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        this.Response.ContentType = result.Value.ContentType;
        this.Response.ContentLength = Encoding.UTF8.GetByteCount(result.Value.Content);
        return new EmptyResult();
    }

    private string GetPublicBaseUrl()
    {
        return this.settings.GetNormalizedPublicBaseUrl(requireHttps: !this.environment.IsDevelopment());
    }

    private IReadOnlyCollection<string> BuildRobotsAllowPaths()
    {
        return this.BuildRobotsPaths(new[] { "/" }.Concat(this.settings.RobotsAllowPaths));
    }

    private IReadOnlyCollection<string> BuildRobotsDisallowPaths()
    {
        return this.BuildRobotsPaths(this.settings.RobotsDisallowPaths);
    }

    private IReadOnlyCollection<string> BuildRobotsPaths(IEnumerable<string> configuredPaths)
    {
        List<string> paths = new List<string>();
        IReadOnlyCollection<string> languages = this.settings.SupportedLanguages.Count > 0
            ? this.settings.SupportedLanguages
            : new[] { this.settings.DefaultLanguage };

        foreach (string configuredPath in configuredPaths)
        {
            if (!configuredPath.Contains("{lang}", StringComparison.OrdinalIgnoreCase))
            {
                paths.Add(configuredPath);
                continue;
            }

            foreach (string language in languages)
            {
                paths.Add(configuredPath.Replace("{lang}", language.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
            }
        }

        return paths
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Select(static path => path.StartsWith('/') ? path : $"/{path}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsIndexNowKeyFileRequest(string requestedPath, SeoSitemapSettings sitemapSettings)
    {
        if (!sitemapSettings.IsIndexNowEnabled || string.IsNullOrWhiteSpace(sitemapSettings.IndexNowKey))
        {
            return false;
        }

        string normalizedRequestedPath = NormalizeIndexNowKeyPath(requestedPath);
        string defaultKeyPath = NormalizeIndexNowKeyPath($"{sitemapSettings.IndexNowKey.Trim()}.txt");
        if (string.Equals(normalizedRequestedPath, defaultKeyPath, StringComparison.Ordinal))
        {
            return true;
        }

        string configuredKeyLocationPath = NormalizeConfiguredIndexNowKeyLocationPath(sitemapSettings.IndexNowKeyLocation);
        return configuredKeyLocationPath.Length > 0 &&
               string.Equals(normalizedRequestedPath, configuredKeyLocationPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeConfiguredIndexNowKeyLocationPath(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            return string.Empty;
        }

        if (Uri.TryCreate(normalizedValue, UriKind.Absolute, out Uri? absoluteUri))
        {
            return NormalizeIndexNowKeyPath(absoluteUri.AbsolutePath);
        }

        return NormalizeIndexNowKeyPath(normalizedValue);
    }

    private static string NormalizeIndexNowKeyPath(string value)
    {
        string normalizedValue = value.Trim();
        int queryIndex = normalizedValue.IndexOfAny(new[] { '?', '#' });
        if (queryIndex >= 0)
        {
            normalizedValue = normalizedValue[..queryIndex];
        }

        return normalizedValue.StartsWith('/') ? normalizedValue : $"/{normalizedValue}";
    }
}
