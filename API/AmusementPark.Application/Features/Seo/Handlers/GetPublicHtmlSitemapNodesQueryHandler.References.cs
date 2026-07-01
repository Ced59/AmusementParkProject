using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Handlers;

public sealed partial class GetPublicHtmlSitemapNodesQueryHandler
{
    private static IReadOnlyCollection<PublicHtmlSitemapNode> BuildReferenceGroupNodes(string language)
    {
        return new List<PublicHtmlSitemapNode>
        {
            new PublicHtmlSitemapNode { Id = "reference-manufacturers", Label = Label(language, "manufacturers"), RelativeUrl = $"/{language}/manufacturers", HasChildren = true },
            new PublicHtmlSitemapNode { Id = "reference-operators", Label = Label(language, "operators"), HasChildren = true },
            new PublicHtmlSitemapNode { Id = "reference-founders", Label = Label(language, "founders"), HasChildren = true },
        };
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildOperatorNodesAsync(
        string language,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkOperator> operators = await this.parkOperatorRepository.GetAllAsync(cancellationToken);
        return operators
            .Where(static entity => !string.IsNullOrWhiteSpace(entity.Id) && !string.IsNullOrWhiteSpace(entity.Name) && entity.AdminReviewStatus != AdminReviewStatus.NotRelevant)
            .OrderBy(static entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entity => CreateLeaf(
                $"operator:{entity.Id}",
                entity.Name,
                $"/{language}/park-operator/{entity.Id}/{SeoSlugService.ToSlug(entity.Name, "reference")}"))
            .ToList();
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildFounderNodesAsync(
        string language,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkFounder> founders = await this.parkFounderRepository.GetAllAsync(cancellationToken);
        return founders
            .Where(static entity => !string.IsNullOrWhiteSpace(entity.Id) && !string.IsNullOrWhiteSpace(entity.Name))
            .OrderBy(static entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entity => CreateLeaf(
                $"founder:{entity.Id}",
                entity.Name,
                $"/{language}/park-founder/{entity.Id}/{SeoSlugService.ToSlug(entity.Name, "reference")}"))
            .ToList();
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildManufacturerNodesAsync(
        string language,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<AttractionManufacturer> manufacturers = await this.attractionManufacturerRepository.GetAllAsync(cancellationToken);
        return manufacturers
            .Where(static entity => !string.IsNullOrWhiteSpace(entity.Id) && !string.IsNullOrWhiteSpace(entity.Name) && entity.IsVisible && entity.AdminReviewStatus != AdminReviewStatus.NotRelevant)
            .OrderBy(static entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entity => CreateLeaf(
                $"manufacturer:{entity.Id}",
                entity.Name,
                $"/{language}/park-manufacturer/{entity.Id}/{SeoSlugService.ToSlug(entity.Name, "reference")}"))
            .ToList();
    }
}
