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

public sealed class TechnicalPagesSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly ITechnicalPageRepository technicalPageRepository;

    public TechnicalPagesSitemapSectionProvider(ITechnicalPageRepository technicalPageRepository)
    {
        this.technicalPageRepository = technicalPageRepository;
    }

    public string Key => SitemapSectionKeys.TechnicalPages;

    public string FileName => "technical-pages.xml";

    public string DisplayName => "Pages techniques";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(context.SupportedLanguages);
        IReadOnlyCollection<TechnicalPage> pages = await this.technicalPageRepository.GetAllAsync(false, cancellationToken);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        DateTime? lastModifiedUtc = pages.Count == 0 ? null : pages.Max(static page => page.UpdatedAtUtc);
        foreach (string language in languages)
        {
            urls.Add(new SitemapUrlEntry($"/{language}/technical", lastModifiedUtc, "weekly", 0.62m));
        }

        foreach (TechnicalPage page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Slug))
            {
                continue;
            }

            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/technical/{page.Slug}", page.UpdatedAtUtc, "monthly", 0.68m));
            }
        }

        return urls;
    }
}
