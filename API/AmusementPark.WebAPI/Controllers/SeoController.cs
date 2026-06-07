using System.Text;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

    public SeoController(
        IOptions<SeoSettings> settings,
        IWebHostEnvironment environment,
        ISeoSitemapSettingsRepository sitemapSettingsRepository,
        IQueryHandler<GetPublicSitemapDocumentQuery, ApplicationResult<SitemapDocumentResult>> getPublicSitemapDocumentQueryHandler)
    {
        this.settings = settings.Value;
        this.environment = environment;
        this.sitemapSettingsRepository = sitemapSettingsRepository;
        this.getPublicSitemapDocumentQueryHandler = getPublicSitemapDocumentQueryHandler;
    }

    [HttpGet("robots.txt")]
    [Produces("text/plain")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult GetRobotsTxt()
    {
        string publicBaseUrl = this.GetPublicBaseUrl();
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("User-agent: *");
        builder.AppendLine("Allow: /");

        foreach (string disallowPath in this.BuildRobotsDisallowPaths())
        {
            builder.Append("Disallow: ").AppendLine(disallowPath);
        }

        builder.AppendLine();
        builder.Append("Sitemap: ").Append(publicBaseUrl).AppendLine("/sitemap.xml");

        return this.Content(builder.ToString(), "text/plain", Encoding.UTF8);
    }

    [HttpGet("sitemap.xml")]
    [Produces("application/xml")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSitemapIndexXml(CancellationToken cancellationToken = default)
    {
        return await this.GetSitemapDocumentAsync(sectionKey: null, cancellationToken);
    }

    [HttpGet("sitemaps/{sectionFileName}")]
    [Produces("application/xml")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSitemapSectionXml([FromRoute] string sectionFileName, CancellationToken cancellationToken = default)
    {
        return await this.GetSitemapDocumentAsync(sectionFileName, cancellationToken);
    }

    [HttpGet("{key}.txt")]
    [Produces("text/plain")]
    public async Task<IActionResult> GetIndexNowKeyFileAsync([FromRoute] string key, CancellationToken cancellationToken = default)
    {
        SeoSitemapSettings sitemapSettings = await this.sitemapSettingsRepository.GetAsync(cancellationToken);
        if (!sitemapSettings.IsIndexNowEnabled || string.IsNullOrWhiteSpace(sitemapSettings.IndexNowKey))
        {
            return this.NotFound();
        }

        if (!string.Equals(key, sitemapSettings.IndexNowKey, StringComparison.Ordinal))
        {
            return this.NotFound();
        }

        return this.Content(sitemapSettings.IndexNowKey, "text/plain", Encoding.UTF8);
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

    private string GetPublicBaseUrl()
    {
        return this.settings.GetNormalizedPublicBaseUrl(requireHttps: !this.environment.IsDevelopment());
    }

    private IReadOnlyCollection<string> BuildRobotsDisallowPaths()
    {
        List<string> paths = new List<string>();
        IReadOnlyCollection<string> languages = this.settings.SupportedLanguages.Count > 0
            ? this.settings.SupportedLanguages
            : new[] { this.settings.DefaultLanguage };

        foreach (string configuredPath in this.settings.RobotsDisallowPaths)
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
}
