using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.TechnicalPages;

namespace AmusementPark.Application.Features.Seo.Services;

/// <summary>
/// Provider des pages publiques statiques.
/// </summary>
public sealed class StaticPagesSitemapSectionProvider : ISitemapSectionProvider
{
    private static readonly IReadOnlyCollection<StaticSitemapPage> StaticPages = new[]
    {
        new StaticSitemapPage("home", "daily", 1.0m),
        new StaticSitemapPage("parks", "daily", 0.9m),
        new StaticSitemapPage("rankings", "daily", 0.82m),
        new StaticSitemapPage("rankings/methodology", "monthly", 0.7m),
        new StaticSitemapPage("manufacturers", "weekly", 0.66m),
        new StaticSitemapPage("about", "monthly", 0.4m),
        new StaticSitemapPage("contact", "monthly", 0.35m),
        new StaticSitemapPage("versions", "monthly", 0.3m),
        new StaticSitemapPage("privacy", "yearly", 0.2m),
    };

    public string Key => SitemapSectionKeys.Static;

    public string FileName => "static.xml";

    public string DisplayName => "Pages statiques";

    public Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (string language in NormalizeLanguages(context.SupportedLanguages))
        {
            foreach (StaticSitemapPage page in StaticPages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/{page.Segment}", null, page.ChangeFrequency, page.Priority));
            }
        }

        return Task.FromResult<IReadOnlyCollection<SitemapUrlEntry>>(urls);
    }

    private static IReadOnlyCollection<string> NormalizeLanguages(IReadOnlyCollection<string> languages)
    {
        List<string> normalizedLanguages = languages
            .Select(static language => language.Trim().ToLowerInvariant())
            .Where(static language => !string.IsNullOrWhiteSpace(language))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalizedLanguages.Count > 0 ? normalizedLanguages : new[] { "en" };
    }


}
