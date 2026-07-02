using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Application.Features.Videos.Ports;

namespace AmusementPark.Application.Features.Seo.Handlers;

public sealed partial class GetPublicHtmlSitemapNodesQueryHandler
    : IQueryHandler<GetPublicHtmlSitemapNodesQuery, ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>>
{
    private const int PublicListPageSize = 100;
    private const int PublicMediaPageSize = 100;

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkOpeningHoursRepository openingHoursRepository;
    private readonly IImageRepository imageRepository;
    private readonly IVideoRepository videoRepository;
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IParkOperatorRepository parkOperatorRepository;
    private readonly IParkFounderRepository parkFounderRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly ITechnicalPageRepository technicalPageRepository;

    public GetPublicHtmlSitemapNodesQueryHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkZoneRepository parkZoneRepository,
        IParkOpeningHoursRepository openingHoursRepository,
        IImageRepository imageRepository,
        IVideoRepository videoRepository,
        IHistoryEventRepository historyEventRepository,
        IParkOperatorRepository parkOperatorRepository,
        IParkFounderRepository parkFounderRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        ITechnicalPageRepository technicalPageRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.imageRepository = imageRepository;
        this.videoRepository = videoRepository;
        this.historyEventRepository = historyEventRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.technicalPageRepository = technicalPageRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>> HandleAsync(
        GetPublicHtmlSitemapNodesQuery query,
        CancellationToken cancellationToken = default)
    {
        string? language = NormalizeLanguage(query.Language, query.SupportedLanguages);
        if (language is null)
        {
            return ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>.Failure(
                ApplicationError.Validation("seo.html-sitemap.language.invalid", "The requested language is not served publicly."));
        }

        string parentNodeId = NormalizeParentNodeId(query.ParentNodeId);
        IReadOnlyCollection<PublicHtmlSitemapNode> nodes = await this.BuildNodesForParentAsync(language, parentNodeId, cancellationToken);
        if (query.IncludeDescendants)
        {
            nodes = await this.BuildNodesWithDescendantsAsync(language, nodes, cancellationToken);
        }

        return ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>.Success(nodes);
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildNodesForParentAsync(
        string language,
        string parentNodeId,
        CancellationToken cancellationToken)
    {
        return parentNodeId switch
        {
            "root" => BuildRootNodes(language),
            "parks" => await this.BuildParkNodesAsync(language, cancellationToken),
            "technical" => await this.BuildTechnicalPageNodesAsync(language, cancellationToken),
            "references" => BuildReferenceGroupNodes(language),
            "reference-operators" => await this.BuildOperatorNodesAsync(language, cancellationToken),
            "reference-founders" => await this.BuildFounderNodesAsync(language, cancellationToken),
            "reference-manufacturers" => await this.BuildManufacturerNodesAsync(language, cancellationToken),
            _ => await this.BuildDynamicNodesAsync(language, parentNodeId, cancellationToken),
        };
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildNodesWithDescendantsAsync(
        string language,
        IReadOnlyCollection<PublicHtmlSitemapNode> nodes,
        CancellationToken cancellationToken)
    {
        List<PublicHtmlSitemapNode> enrichedNodes = new List<PublicHtmlSitemapNode>(nodes.Count);
        foreach (PublicHtmlSitemapNode node in nodes)
        {
            if (!node.HasChildren)
            {
                enrichedNodes.Add(node);
                continue;
            }

            IReadOnlyCollection<PublicHtmlSitemapNode> childNodes = await this.BuildNodesForParentAsync(language, node.Id, cancellationToken);
            IReadOnlyCollection<PublicHtmlSitemapNode> descendantNodes = await this.BuildNodesWithDescendantsAsync(language, childNodes, cancellationToken);
            enrichedNodes.Add(new PublicHtmlSitemapNode
            {
                Id = node.Id,
                Label = node.Label,
                RelativeUrl = node.RelativeUrl,
                HasChildren = node.HasChildren,
                Children = descendantNodes,
            });
        }

        return enrichedNodes;
    }

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildDynamicNodesAsync(
        string language,
        string parentNodeId,
        CancellationToken cancellationToken)
    {
        string? parkId = TryReadNodeValue(parentNodeId, "park");
        if (parkId is not null)
        {
            return await this.BuildParkChildNodesAsync(language, parkId, cancellationToken);
        }

        parkId = TryReadNodeValue(parentNodeId, "park-items");
        if (parkId is not null)
        {
            return await this.BuildParkItemNodesAsync(language, parkId, cancellationToken);
        }

        parkId = TryReadNodeValue(parentNodeId, "park-zones");
        if (parkId is not null)
        {
            return await this.BuildParkZoneNodesAsync(language, parkId, cancellationToken);
        }

        parkId = TryReadNodeValue(parentNodeId, "park-videos");
        if (parkId is not null)
        {
            return await this.BuildParkVideoNodesAsync(language, parkId, cancellationToken);
        }

        parkId = TryReadNodeValue(parentNodeId, "park-history");
        if (parkId is not null)
        {
            return await this.BuildParkHistoryArticleNodesAsync(language, parkId, cancellationToken);
        }

        string? itemId = TryReadNodeValue(parentNodeId, "park-item");
        if (itemId is not null)
        {
            return await this.BuildParkItemChildNodesAsync(language, itemId, cancellationToken);
        }

        itemId = TryReadNodeValue(parentNodeId, "park-item-videos");
        if (itemId is not null)
        {
            return await this.BuildParkItemVideoNodesAsync(language, itemId, cancellationToken);
        }

        itemId = TryReadNodeValue(parentNodeId, "park-item-history");
        if (itemId is not null)
        {
            return await this.BuildParkItemHistoryArticleNodesAsync(language, itemId, cancellationToken);
        }

        return Array.Empty<PublicHtmlSitemapNode>();
    }

    private static IReadOnlyCollection<PublicHtmlSitemapNode> BuildRootNodes(string language)
    {
        return new List<PublicHtmlSitemapNode>
        {
            CreateLeaf("home", Label(language, "home"), $"/{language}/home"),
            new PublicHtmlSitemapNode { Id = "parks", Label = Label(language, "parks"), RelativeUrl = $"/{language}/parks", HasChildren = true },
            new PublicHtmlSitemapNode { Id = "technical", Label = Label(language, "technical"), RelativeUrl = $"/{language}/technical", HasChildren = true },
            new PublicHtmlSitemapNode { Id = "references", Label = Label(language, "references"), HasChildren = true },
            CreateLeaf("rankings", Label(language, "rankings"), $"/{language}/rankings"),
            CreateLeaf("about", Label(language, "about"), $"/{language}/about"),
            CreateLeaf("contact", Label(language, "contact"), $"/{language}/contact"),
            CreateLeaf("versions", Label(language, "versions"), $"/{language}/versions"),
            CreateLeaf("privacy", Label(language, "privacy"), $"/{language}/privacy"),
            CreateLeaf("sitemap", Label(language, "sitemap"), $"/{language}/sitemap"),
        };
    }

    private static string NormalizeParentNodeId(string? parentNodeId)
    {
        return string.IsNullOrWhiteSpace(parentNodeId) ? "root" : parentNodeId.Trim();
    }
}
