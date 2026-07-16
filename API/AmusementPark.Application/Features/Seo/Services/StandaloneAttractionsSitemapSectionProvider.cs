using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Services;

public sealed class StandaloneAttractionsSitemapSectionProvider : ISitemapSectionProvider
{
    private const int MaxStandaloneAttractionUrls = 50_000;

    private readonly IStandaloneAttractionRepository standaloneAttractionRepository;

    public StandaloneAttractionsSitemapSectionProvider(IStandaloneAttractionRepository standaloneAttractionRepository)
    {
        this.standaloneAttractionRepository = standaloneAttractionRepository;
    }

    public string Key => SitemapSectionKeys.StandaloneAttractions;

    public string FileName => "standalone-attractions.xml";

    public string DisplayName => "Attractions autonomes";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(context.SupportedLanguages);
        IReadOnlyCollection<StandaloneAttraction> attractions = await this.standaloneAttractionRepository.GetPublicSitemapCandidatesAsync(
            MaxStandaloneAttractionUrls,
            cancellationToken);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (StandaloneAttraction attraction in attractions)
        {
            if (!attraction.IsPubliclyPublishable())
            {
                continue;
            }

            string slug = SeoSlugService.ToSlug(attraction.Name, "attraction");
            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/attraction/{attraction.Id}/{slug}", attraction.UpdatedAtUtc, "weekly", 0.72m));
            }
        }

        return urls;
    }
}
