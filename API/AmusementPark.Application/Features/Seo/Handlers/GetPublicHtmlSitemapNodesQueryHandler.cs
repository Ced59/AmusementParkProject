using System.Xml;
using System.Xml.Linq;
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
using AmusementPark.Application.Features.Seo.Ports;
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
    private readonly ISeoSitemapSnapshotRepository sitemapSnapshotRepository;

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
        ITechnicalPageRepository technicalPageRepository,
        ISeoSitemapSnapshotRepository sitemapSnapshotRepository)
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
        this.sitemapSnapshotRepository = sitemapSnapshotRepository;
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
        if (query.IncludeDescendants && string.Equals(parentNodeId, "root", StringComparison.OrdinalIgnoreCase))
        {
            IReadOnlyCollection<PublicHtmlSitemapNode> sitemapNodes = await this.BuildSnapshotLinkNodesAsync(language, query.SupportedLanguages, cancellationToken);
            return ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>.Success(sitemapNodes);
        }

        IReadOnlyCollection<PublicHtmlSitemapNode> nodes = await this.BuildNodesForParentAsync(language, parentNodeId, cancellationToken);

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

    private async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildSnapshotLinkNodesAsync(
        string language,
        IReadOnlyCollection<string> supportedLanguages,
        CancellationToken cancellationToken)
    {
        SitemapSnapshot? snapshot = await this.sitemapSnapshotRepository.GetLatestAsync(cancellationToken);
        if (snapshot is null || snapshot.Sections.Count == 0)
        {
            return BuildRootNodes(language);
        }

        List<PublicHtmlSitemapNode> sectionNodes = new List<PublicHtmlSitemapNode>();
        HashSet<string> emittedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyCollection<string> normalizedSupportedLanguages = NormalizeSupportedLanguages(supportedLanguages);
        foreach (SitemapSectionStats section in snapshot.Sections
                     .Where(section => IsSnapshotSectionRelevantForLanguage(section.Key, language, normalizedSupportedLanguages))
                     .OrderBy(static section => section.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            string? sectionXml = await this.sitemapSnapshotRepository.GetSectionXmlAsync(section.Key, cancellationToken);
            if (string.IsNullOrWhiteSpace(sectionXml))
            {
                continue;
            }

            IReadOnlyCollection<PublicHtmlSitemapNode> linkNodes = ExtractSitemapLinkNodes(section.Key, sectionXml, language, emittedUrls);
            if (linkNodes.Count == 0)
            {
                continue;
            }

            sectionNodes.Add(new PublicHtmlSitemapNode
            {
                Id = $"sitemap-section:{section.Key}",
                Label = section.DisplayName,
                HasChildren = true,
                Children = linkNodes,
            });
        }

        return sectionNodes.Count == 0 ? BuildRootNodes(language) : sectionNodes;
    }

    private static IReadOnlyCollection<string> NormalizeSupportedLanguages(IReadOnlyCollection<string> supportedLanguages)
    {
        return supportedLanguages.Count == 0
            ? new[] { "en" }
            : supportedLanguages
                .Where(static supportedLanguage => !string.IsNullOrWhiteSpace(supportedLanguage))
                .Select(static supportedLanguage => supportedLanguage.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    private static bool IsSnapshotSectionRelevantForLanguage(
        string sectionKey,
        string language,
        IReadOnlyCollection<string> supportedLanguages)
    {
        string normalizedKey = sectionKey.Trim();
        if (normalizedKey.Length == 0)
        {
            return false;
        }

        if (HasLanguageScopedSectionSuffix(normalizedKey, language))
        {
            return true;
        }

        foreach (string supportedLanguage in supportedLanguages)
        {
            if (!string.Equals(supportedLanguage, language, StringComparison.OrdinalIgnoreCase)
                && HasLanguageScopedSectionSuffix(normalizedKey, supportedLanguage))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasLanguageScopedSectionSuffix(string sectionKey, string language)
    {
        string languageSuffix = $"-{language}";
        if (sectionKey.EndsWith(languageSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string chunkedLanguageSuffix = $"{languageSuffix}-";
        int index = sectionKey.LastIndexOf(chunkedLanguageSuffix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return false;
        }

        string chunkNumber = sectionKey[(index + chunkedLanguageSuffix.Length)..];
        return chunkNumber.Length > 0 && chunkNumber.All(static value => value >= '0' && value <= '9');
    }

    private static IReadOnlyCollection<PublicHtmlSitemapNode> ExtractSitemapLinkNodes(
        string sectionKey,
        string sectionXml,
        string language,
        HashSet<string> emittedUrls)
    {
        try
        {
            XDocument document = XDocument.Parse(sectionXml, LoadOptions.None);
            XNamespace sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
            string languagePrefix = $"/{language}/";
            List<PublicHtmlSitemapNode> nodes = new List<PublicHtmlSitemapNode>();
            int index = 0;

            foreach (XElement locElement in document.Descendants(sitemapNamespace + "url").Elements(sitemapNamespace + "loc"))
            {
                string? relativeUrl = TryCreateCurrentLanguageRelativeUrl(locElement.Value, languagePrefix);
                if (relativeUrl is null || !emittedUrls.Add(relativeUrl))
                {
                    continue;
                }

                nodes.Add(CreateLeaf(
                    $"sitemap-link:{sectionKey}:{index}",
                    CreateLinkLabel(relativeUrl, languagePrefix),
                    relativeUrl));
                index++;
            }

            return nodes
                .OrderBy(static node => node.RelativeUrl, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (XmlException)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }
    }

    private static string? TryCreateCurrentLanguageRelativeUrl(string value, string languagePrefix)
    {
        string locValue = value.Trim();
        if (!Uri.TryCreate(locValue, UriKind.Absolute, out Uri? absoluteUri))
        {
            return null;
        }

        string relativeUrl = absoluteUri.AbsolutePath;
        return relativeUrl.StartsWith(languagePrefix, StringComparison.OrdinalIgnoreCase) ? relativeUrl : null;
    }

    private static string CreateLinkLabel(string relativeUrl, string languagePrefix)
    {
        string label = relativeUrl.StartsWith(languagePrefix, StringComparison.OrdinalIgnoreCase)
            ? relativeUrl[languagePrefix.Length..]
            : relativeUrl.TrimStart('/');

        return label.Length == 0 ? relativeUrl : label.Replace('/', ' ');
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
