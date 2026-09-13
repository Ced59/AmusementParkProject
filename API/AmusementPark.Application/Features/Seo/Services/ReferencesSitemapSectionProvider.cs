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
/// Provider des références publiques : exploitants, fondateurs et constructeurs.
/// </summary>
public sealed class ReferencesSitemapSectionProvider : ISitemapSectionProvider
{
    private readonly IParkOperatorRepository parkOperatorRepository;
    private readonly IParkFounderRepository parkFounderRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;

    public ReferencesSitemapSectionProvider(
        IParkOperatorRepository parkOperatorRepository,
        IParkFounderRepository parkFounderRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository)
    {
        this.parkOperatorRepository = parkOperatorRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
    }

    public string Key => SitemapSectionKeys.References;

    public string FileName => "references.xml";

    public string DisplayName => "Références";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(context.SupportedLanguages);
        List<SitemapUrlEntry> urls = new List<SitemapUrlEntry>();

        IReadOnlyCollection<ParkOperator> operators = await this.parkOperatorRepository.GetAllAsync(cancellationToken);
        foreach (ParkOperator entity in operators.Where(static entity => entity.AdminReviewStatus != AdminReviewStatus.NotRelevant))
        {
            AddReferenceUrls(urls, languages, "park-operator", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }

        IReadOnlyCollection<ParkFounder> founders = await this.parkFounderRepository.GetAllAsync(cancellationToken);
        foreach (ParkFounder entity in founders)
        {
            AddReferenceUrls(urls, languages, "park-founder", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }

        IReadOnlyCollection<AttractionManufacturer> manufacturers = await this.attractionManufacturerRepository.GetAllAsync(cancellationToken);
        foreach (AttractionManufacturer entity in manufacturers.Where(static entity => entity.IsVisible && entity.AdminReviewStatus != AdminReviewStatus.NotRelevant))
        {
            AddReferenceUrls(urls, languages, "park-manufacturer", entity.Id, entity.Name, entity.UpdatedAtUtc);
        }

        return urls;
    }

    private static void AddReferenceUrls(List<SitemapUrlEntry> urls, IReadOnlyCollection<string> languages, string routeSegment, string? id, string? name, DateTime? lastModifiedUtc)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        string slug = SeoSlugService.ToSlug(name, "reference");
        foreach (string language in languages)
        {
            urls.Add(new SitemapUrlEntry($"/{language}/{routeSegment}/{id}/{slug}", lastModifiedUtc, "monthly", 0.55m));
        }
    }
}
