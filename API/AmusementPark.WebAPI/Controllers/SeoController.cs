using System.Text;
using System.Xml;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Queries;
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
    private readonly IQueryHandler<GetPublicSitemapSeedQuery, ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>>> getPublicSitemapSeedQueryHandler;

    public SeoController(
        IOptions<SeoSettings> settings,
        IWebHostEnvironment environment,
        IQueryHandler<GetPublicSitemapSeedQuery, ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>>> getPublicSitemapSeedQueryHandler)
    {
        this.settings = settings.Value;
        this.environment = environment;
        this.getPublicSitemapSeedQueryHandler = getPublicSitemapSeedQueryHandler;
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
    public async Task<IActionResult> GetSitemapXml(CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<PublicSitemapUrl>> result = await this.getPublicSitemapSeedQueryHandler.HandleAsync(
            new GetPublicSitemapSeedQuery(this.settings.SupportedLanguages, this.settings.MaxDynamicUrlsPerType),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        string xml = this.BuildSitemapXml(result.Value);
        return this.Content(xml, "application/xml", Encoding.UTF8);
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

    private string BuildSitemapXml(IReadOnlyCollection<PublicSitemapUrl> urls)
    {
        string publicBaseUrl = this.GetPublicBaseUrl();
        StringBuilder builder = new StringBuilder();
        XmlWriterSettings writerSettings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            OmitXmlDeclaration = true,
        };

        using XmlWriter writer = XmlWriter.Create(builder, writerSettings);
        writer.WriteStartDocument();
        writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

        foreach (PublicSitemapUrl url in urls)
        {
            writer.WriteStartElement("url");
            writer.WriteElementString("loc", $"{publicBaseUrl}{url.RelativePath}");

            if (url.LastModifiedUtc.HasValue)
            {
                writer.WriteElementString("lastmod", url.LastModifiedUtc.Value.ToUniversalTime().ToString("yyyy-MM-dd"));
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
        return builder.ToString();
    }
}
