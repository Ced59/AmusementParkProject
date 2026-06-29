using AmusementPark.Application.Features.ParkOpeningHours.Contracts;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Services;

public sealed class ParkOpeningHoursSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IParkRepository parkRepository;
    private readonly IParkOpeningHoursRepository openingHoursRepository;

    public ParkOpeningHoursSitemapSectionProvider(
        IParkRepository parkRepository,
        IParkOpeningHoursRepository openingHoursRepository)
    {
        this.parkRepository = parkRepository;
        this.openingHoursRepository = openingHoursRepository;
    }

    public string Key => SitemapSectionKeys.ParkOpeningHours;

    public string FileName => "park-opening-hours.xml";

    public string DisplayName => "Horaires des parcs";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(context.SupportedLanguages);
        IReadOnlyCollection<Park> publicParks = await SitemapPublicCandidateLoader.LoadPublicParksAsync(
            this.parkRepository,
            cancellationToken);
        List<string> parkIds = publicParks
            .Select(static park => park.Id)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId!)
            .ToList();
        IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary> summaries = await this.openingHoursRepository.GetSummariesByParkIdsAsync(parkIds, cancellationToken);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (Park park in publicParks.OrderBy(static park => park.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(park.Id)
                || !summaries.TryGetValue(park.Id, out ParkOpeningHoursScheduleSummary? summary)
                || !summary.HasScheduleData)
            {
                continue;
            }

            string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
            DateTime? lastModifiedUtc = ResolveLatest(park.UpdatedAtUtc, summary.UpdatedAtUtc);
            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{parkSlug}/opening-hours", lastModifiedUtc, "weekly", 0.77m));
            }
        }

        return urls;
    }

    private static DateTime? ResolveLatest(DateTime? first, DateTime? second)
    {
        if (!first.HasValue)
        {
            return second;
        }

        if (!second.HasValue)
        {
            return first;
        }

        return first.Value > second.Value ? first : second;
    }
}
