using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.Seo.Services;

public sealed class ParkPricingSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IParkRepository parkRepository;
    private readonly IParkPricingRepository pricingRepository;

    public ParkPricingSitemapSectionProvider(
        IParkRepository parkRepository,
        IParkPricingRepository pricingRepository)
    {
        this.parkRepository = parkRepository;
        this.pricingRepository = pricingRepository;
    }

    public string Key => SitemapSectionKeys.ParkPricing;

    public string FileName => "park-pricing.xml";

    public string DisplayName => "Tarifs des parcs";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(
        SitemapGenerationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(context.SupportedLanguages);
        IReadOnlyCollection<Park> publicParks = await SitemapPublicCandidateLoader.LoadPublicParksAsync(
            this.parkRepository,
            cancellationToken);
        IReadOnlyDictionary<string, ParkPricingEntity> currentPricingByParkId = await LoadCurrentPricingByParkIdsAsync(
            this.pricingRepository,
            publicParks.Select(static park => park.Id).Where(static parkId => !string.IsNullOrWhiteSpace(parkId)).Select(static parkId => parkId!).ToList(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);

        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();
        foreach (Park park in publicParks.OrderBy(static park => park.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!park.Status.IsOpenToVisitors()
                || string.IsNullOrWhiteSpace(park.Id)
                || !currentPricingByParkId.TryGetValue(park.Id, out ParkPricingEntity? pricing))
            {
                continue;
            }

            string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
            DateTime? lastModifiedUtc = ResolveLatest(park.UpdatedAtUtc, pricing.UpdatedAtUtc);
            foreach (string language in languages)
            {
                urls.Add(new SitemapUrlEntry($"/{language}/park/{park.Id}/{parkSlug}/pricing", lastModifiedUtc, "weekly", 0.8m));
            }
        }

        return urls;
    }

    internal static async Task<IReadOnlyDictionary<string, ParkPricingEntity>> LoadCurrentPricingByParkIdsAsync(
        IParkPricingRepository pricingRepository,
        IReadOnlyCollection<string> parkIds,
        DateOnly currentDate,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkPricingEntity> pricing = await pricingRepository.GetByParkIdsAsync(parkIds, cancellationToken);
        return pricing
            .Where(item => !string.IsNullOrWhiteSpace(item.ParkId) && item.HasPricedOffersValidOn(currentDate))
            .GroupBy(static item => item.ParkId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static DateTime? ResolveLatest(DateTime? parkUpdatedAtUtc, DateTime pricingUpdatedAtUtc)
    {
        if (!parkUpdatedAtUtc.HasValue)
        {
            return pricingUpdatedAtUtc;
        }

        return parkUpdatedAtUtc.Value > pricingUpdatedAtUtc ? parkUpdatedAtUtc : pricingUpdatedAtUtc;
    }
}
