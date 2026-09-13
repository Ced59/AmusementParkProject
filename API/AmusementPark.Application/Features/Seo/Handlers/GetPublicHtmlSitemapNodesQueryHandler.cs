using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Core.Localization;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using AmusementPark.Core.Domain.TechnicalPages;
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
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Application.Features.Videos.Ports;

namespace AmusementPark.Application.Features.Seo.Handlers;
public sealed class GetPublicHtmlSitemapNodesQueryHandler : IQueryHandler<GetPublicHtmlSitemapNodesQuery, ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>>
{
    internal static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SiteLabels = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["home"] = "Home",
            ["parks"] = "Parks",
            ["technical"] = "Technical pages",
            ["references"] = "References",
            ["rankings"] = "Rankings",
            ["ratingMethodology"] = "Ranking methodology",
            ["about"] = "About",
            ["contact"] = "Contact",
            ["versions"] = "Versions",
            ["privacy"] = "Privacy",
            ["sitemap"] = "Sitemap",
            ["park"] = "Park",
            ["interactiveMap"] = "Map",
            ["weather"] = "Weather",
            ["openingHours"] = "Opening hours",
            ["pricing"] = "Tickets and prices",
            ["images"] = "Images",
            ["videos"] = "Videos",
            ["zones"] = "Areas",
            ["items"] = "Attractions",
            ["history"] = "History",
            ["manufacturers"] = "Manufacturers",
            ["operators"] = "Operators",
            ["founders"] = "Founders",
        },
        ["fr"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["home"] = "Accueil",
            ["parks"] = "Parcs",
            ["technical"] = "Pages techniques",
            ["references"] = "Références",
            ["rankings"] = "Classements",
            ["ratingMethodology"] = "Méthodologie des classements",
            ["about"] = "À propos",
            ["contact"] = "Contact",
            ["versions"] = "Versions",
            ["privacy"] = "Confidentialité",
            ["sitemap"] = "Plan du site",
            ["park"] = "Parc",
            ["interactiveMap"] = "Carte",
            ["weather"] = "Météo",
            ["openingHours"] = "Horaires",
            ["pricing"] = "Tarifs et billets",
            ["images"] = "Images",
            ["videos"] = "Vidéos",
            ["zones"] = "Zones",
            ["items"] = "Attractions",
            ["history"] = "Histoire",
            ["manufacturers"] = "Constructeurs",
            ["operators"] = "Exploitants",
            ["founders"] = "Fondateurs",
        },
        ["de"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["home"] = "Startseite",
            ["parks"] = "Parks",
            ["technical"] = "Technische Seiten",
            ["references"] = "Referenzen",
            ["rankings"] = "Ranglisten",
            ["ratingMethodology"] = "Ranglisten-Methodik",
            ["about"] = "Über uns",
            ["contact"] = "Kontakt",
            ["versions"] = "Versionen",
            ["privacy"] = "Datenschutz",
            ["sitemap"] = "Sitemap",
            ["park"] = "Park",
            ["interactiveMap"] = "Karte",
            ["weather"] = "Wetter",
            ["openingHours"] = "Öffnungszeiten",
            ["pricing"] = "Preise und Tickets",
            ["images"] = "Bilder",
            ["videos"] = "Videos",
            ["zones"] = "Bereiche",
            ["items"] = "Attraktionen",
            ["history"] = "Geschichte",
            ["manufacturers"] = "Hersteller",
            ["operators"] = "Betreiber",
            ["founders"] = "Gründer",
        },
        ["es"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["home"] = "Inicio",
            ["parks"] = "Parques",
            ["technical"] = "Páginas técnicas",
            ["references"] = "Referencias",
            ["rankings"] = "Clasificaciones",
            ["ratingMethodology"] = "Metodología de las clasificaciones",
            ["about"] = "Acerca de",
            ["contact"] = "Contacto",
            ["versions"] = "Versiones",
            ["privacy"] = "Privacidad",
            ["sitemap"] = "Mapa del sitio",
            ["park"] = "Parque",
            ["interactiveMap"] = "Mapa",
            ["weather"] = "Tiempo",
            ["openingHours"] = "Horarios",
            ["pricing"] = "Tarifas y entradas",
            ["images"] = "Imágenes",
            ["videos"] = "Vídeos",
            ["zones"] = "Zonas",
            ["items"] = "Atracciones",
            ["history"] = "Historia",
            ["manufacturers"] = "Fabricantes",
            ["operators"] = "Operadores",
            ["founders"] = "Fundadores",
        },
        ["it"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["home"] = "Home",
            ["parks"] = "Parchi",
            ["technical"] = "Pagine tecniche",
            ["references"] = "Riferimenti",
            ["rankings"] = "Classifiche",
            ["ratingMethodology"] = "Metodologia delle classifiche",
            ["about"] = "Chi siamo",
            ["contact"] = "Contatto",
            ["versions"] = "Versioni",
            ["privacy"] = "Privacy",
            ["sitemap"] = "Mappa del sito",
            ["park"] = "Parco",
            ["interactiveMap"] = "Mappa",
            ["weather"] = "Meteo",
            ["openingHours"] = "Orari",
            ["pricing"] = "Tariffe e biglietti",
            ["images"] = "Immagini",
            ["videos"] = "Video",
            ["zones"] = "Zone",
            ["items"] = "Attrazioni",
            ["history"] = "Storia",
            ["manufacturers"] = "Costruttori",
            ["operators"] = "Operatori",
            ["founders"] = "Fondatori",
        },
        ["nl"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["home"] = "Home",
            ["parks"] = "Parken",
            ["technical"] = "Technische pagina's",
            ["references"] = "Referenties",
            ["rankings"] = "Ranglijsten",
            ["ratingMethodology"] = "Methodologie van ranglijsten",
            ["about"] = "Over ons",
            ["contact"] = "Contact",
            ["versions"] = "Versies",
            ["privacy"] = "Privacy",
            ["sitemap"] = "Sitemap",
            ["park"] = "Park",
            ["interactiveMap"] = "Kaart",
            ["weather"] = "Weer",
            ["openingHours"] = "Openingstijden",
            ["pricing"] = "Prijzen en tickets",
            ["images"] = "Afbeeldingen",
            ["videos"] = "Video's",
            ["zones"] = "Zones",
            ["items"] = "Attracties",
            ["history"] = "Geschiedenis",
            ["manufacturers"] = "Bouwers",
            ["operators"] = "Exploitanten",
            ["founders"] = "Oprichters",
        },
        ["pl"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["home"] = "Strona główna",
            ["parks"] = "Parki",
            ["technical"] = "Strony techniczne",
            ["references"] = "Referencje",
            ["rankings"] = "Rankingi",
            ["ratingMethodology"] = "Metodologia rankingów",
            ["about"] = "O nas",
            ["contact"] = "Kontakt",
            ["versions"] = "Wersje",
            ["privacy"] = "Prywatność",
            ["sitemap"] = "Mapa strony",
            ["park"] = "Park",
            ["interactiveMap"] = "Mapa",
            ["weather"] = "Pogoda",
            ["openingHours"] = "Godziny otwarcia",
            ["pricing"] = "Ceny i bilety",
            ["images"] = "Obrazy",
            ["videos"] = "Filmy",
            ["zones"] = "Strefy",
            ["items"] = "Atrakcje",
            ["history"] = "Historia",
            ["manufacturers"] = "Producenci",
            ["operators"] = "Operatorzy",
            ["founders"] = "Założyciele",
        },
        ["pt"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["home"] = "Início",
            ["parks"] = "Parques",
            ["technical"] = "Páginas técnicas",
            ["references"] = "Referências",
            ["rankings"] = "Classificações",
            ["ratingMethodology"] = "Metodologia das classificações",
            ["about"] = "Sobre",
            ["contact"] = "Contacto",
            ["versions"] = "Versões",
            ["privacy"] = "Privacidade",
            ["sitemap"] = "Mapa do site",
            ["park"] = "Parque",
            ["interactiveMap"] = "Mapa",
            ["weather"] = "Meteorologia",
            ["openingHours"] = "Horários",
            ["pricing"] = "Preços e bilhetes",
            ["images"] = "Imagens",
            ["videos"] = "Vídeos",
            ["zones"] = "Zonas",
            ["items"] = "Atrações",
            ["history"] = "História",
            ["manufacturers"] = "Fabricantes",
            ["operators"] = "Operadores",
            ["founders"] = "Fundadores",
        },
    };
    internal const int PublicListPageSize = 100;
    internal const int PublicMediaPageSize = 100;
    internal readonly IParkRepository parkRepository;
    internal readonly IParkItemRepository parkItemRepository;
    internal readonly IParkZoneRepository parkZoneRepository;
    internal readonly IParkOpeningHoursRepository openingHoursRepository;
    internal readonly IParkPricingRepository pricingRepository;
    internal readonly IImageRepository imageRepository;
    internal readonly IVideoRepository videoRepository;
    internal readonly IHistoryEventRepository historyEventRepository;
    internal readonly IParkOperatorRepository parkOperatorRepository;
    internal readonly IParkFounderRepository parkFounderRepository;
    internal readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    internal readonly ITechnicalPageRepository technicalPageRepository;
    internal readonly ISeoSitemapSnapshotRepository sitemapSnapshotRepository;
    public GetPublicHtmlSitemapNodesQueryHandler(IParkRepository parkRepository, IParkItemRepository parkItemRepository, IParkZoneRepository parkZoneRepository, IParkOpeningHoursRepository openingHoursRepository, IParkPricingRepository pricingRepository, IImageRepository imageRepository, IVideoRepository videoRepository, IHistoryEventRepository historyEventRepository, IParkOperatorRepository parkOperatorRepository, IParkFounderRepository parkFounderRepository, IAttractionManufacturerRepository attractionManufacturerRepository, ITechnicalPageRepository technicalPageRepository, ISeoSitemapSnapshotRepository sitemapSnapshotRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.pricingRepository = pricingRepository;
        this.imageRepository = imageRepository;
        this.videoRepository = videoRepository;
        this.historyEventRepository = historyEventRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.technicalPageRepository = technicalPageRepository;
        this.sitemapSnapshotRepository = sitemapSnapshotRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>> HandleAsync(GetPublicHtmlSitemapNodesQuery query, CancellationToken cancellationToken = default)
    {
        string? language = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.NormalizeLanguage(query.Language, query.SupportedLanguages);
        if (language is null)
        {
            return ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>.Failure(ApplicationError.Validation("seo.html-sitemap.language.invalid", "The requested language is not served publicly."));
        }

        string parentNodeId = GetPublicHtmlSitemapNodesQueryHandler.NormalizeParentNodeId(query.ParentNodeId);
        if (query.IncludeDescendants && string.Equals(parentNodeId, "root", StringComparison.OrdinalIgnoreCase))
        {
            IReadOnlyCollection<PublicHtmlSitemapNode> sitemapNodes = await this.BuildSnapshotLinkNodesAsync(language, query.SupportedLanguages, cancellationToken);
            return ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>.Success(sitemapNodes);
        }

        IReadOnlyCollection<PublicHtmlSitemapNode> nodes = await this.BuildNodesForParentAsync(language, parentNodeId, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>.Success(nodes);
    }

    internal async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildNodesForParentAsync(string language, string parentNodeId, CancellationToken cancellationToken)
    {
        return parentNodeId switch
        {
            "root" => GetPublicHtmlSitemapNodesQueryHandler.BuildRootNodes(language),
            "parks" => await this.BuildParkNodesAsync(language, cancellationToken),
            "technical" => await this.BuildTechnicalPageNodesAsync(language, cancellationToken),
            "references" => GetPublicHtmlSitemapNodesQueryHandlerReferencesExtensions.BuildReferenceGroupNodes(language),
            "reference-operators" => await this.BuildOperatorNodesAsync(language, cancellationToken),
            "reference-founders" => await this.BuildFounderNodesAsync(language, cancellationToken),
            "reference-manufacturers" => await this.BuildManufacturerNodesAsync(language, cancellationToken),
            _ => await this.BuildDynamicNodesAsync(language, parentNodeId, cancellationToken),
        };
    }

    internal async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildSnapshotLinkNodesAsync(string language, IReadOnlyCollection<string> supportedLanguages, CancellationToken cancellationToken)
    {
        SitemapSnapshot? snapshot = await this.sitemapSnapshotRepository.GetLatestAsync(cancellationToken);
        if (snapshot is null || snapshot.Sections.Count == 0)
        {
            return GetPublicHtmlSitemapNodesQueryHandler.BuildRootNodes(language);
        }

        List<PublicHtmlSitemapNode> sectionNodes = new List<PublicHtmlSitemapNode>();
        HashSet<string> emittedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyCollection<string> normalizedSupportedLanguages = GetPublicHtmlSitemapNodesQueryHandler.NormalizeSupportedLanguages(supportedLanguages);
        foreach (SitemapSectionStats section in snapshot.Sections.Where(section => GetPublicHtmlSitemapNodesQueryHandler.IsSnapshotSectionRelevantForLanguage(section.Key, language, normalizedSupportedLanguages)).OrderBy(static section => section.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            string? sectionXml = await this.sitemapSnapshotRepository.GetSectionXmlAsync(section.Key, cancellationToken);
            if (string.IsNullOrWhiteSpace(sectionXml))
            {
                continue;
            }

            IReadOnlyCollection<PublicHtmlSitemapNode> linkNodes = GetPublicHtmlSitemapNodesQueryHandler.ExtractSitemapLinkNodes(section.Key, sectionXml, language, emittedUrls);
            if (linkNodes.Count == 0)
            {
                continue;
            }

            sectionNodes.Add(new PublicHtmlSitemapNode { Id = $"sitemap-section:{section.Key}", Label = section.DisplayName, HasChildren = true, Children = linkNodes, });
        }

        return sectionNodes.Count == 0 ? GetPublicHtmlSitemapNodesQueryHandler.BuildRootNodes(language) : sectionNodes;
    }

    internal static IReadOnlyCollection<string> NormalizeSupportedLanguages(IReadOnlyCollection<string> supportedLanguages)
    {
        return supportedLanguages.Count == 0 ? new[]
        {
            "en"
        }

        : supportedLanguages.Where(static supportedLanguage => !string.IsNullOrWhiteSpace(supportedLanguage)).Select(static supportedLanguage => supportedLanguage.Trim().ToLowerInvariant()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    internal static bool IsSnapshotSectionRelevantForLanguage(string sectionKey, string language, IReadOnlyCollection<string> supportedLanguages)
    {
        string normalizedKey = sectionKey.Trim();
        if (normalizedKey.Length == 0)
        {
            return false;
        }

        if (GetPublicHtmlSitemapNodesQueryHandler.HasLanguageScopedSectionSuffix(normalizedKey, language))
        {
            return true;
        }

        foreach (string supportedLanguage in supportedLanguages)
        {
            if (!string.Equals(supportedLanguage, language, StringComparison.OrdinalIgnoreCase) && GetPublicHtmlSitemapNodesQueryHandler.HasLanguageScopedSectionSuffix(normalizedKey, supportedLanguage))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool HasLanguageScopedSectionSuffix(string sectionKey, string language)
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

    internal static IReadOnlyCollection<PublicHtmlSitemapNode> ExtractSitemapLinkNodes(string sectionKey, string sectionXml, string language, HashSet<string> emittedUrls)
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
                string? relativeUrl = GetPublicHtmlSitemapNodesQueryHandler.TryCreateCurrentLanguageRelativeUrl(locElement.Value, languagePrefix);
                if (relativeUrl is null || !emittedUrls.Add(relativeUrl))
                {
                    continue;
                }

                nodes.Add(GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf($"sitemap-link:{sectionKey}:{index}", GetPublicHtmlSitemapNodesQueryHandler.CreateLinkLabel(relativeUrl, languagePrefix), relativeUrl));
                index++;
            }

            return nodes.OrderBy(static node => node.RelativeUrl, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (XmlException)
        {
            return Array.Empty<PublicHtmlSitemapNode>();
        }
    }

    internal static string? TryCreateCurrentLanguageRelativeUrl(string value, string languagePrefix)
    {
        string locValue = value.Trim();
        if (!Uri.TryCreate(locValue, UriKind.Absolute, out Uri? absoluteUri))
        {
            return null;
        }

        string relativeUrl = absoluteUri.AbsolutePath;
        return relativeUrl.StartsWith(languagePrefix, StringComparison.OrdinalIgnoreCase) ? relativeUrl : null;
    }

    internal static string CreateLinkLabel(string relativeUrl, string languagePrefix)
    {
        string label = relativeUrl.StartsWith(languagePrefix, StringComparison.OrdinalIgnoreCase) ? relativeUrl[languagePrefix.Length..] : relativeUrl.TrimStart('/');
        return label.Length == 0 ? relativeUrl : label.Replace('/', ' ');
    }

    internal async Task<IReadOnlyCollection<PublicHtmlSitemapNode>> BuildDynamicNodesAsync(string language, string parentNodeId, CancellationToken cancellationToken)
    {
        string? parkId = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.TryReadNodeValue(parentNodeId, "park");
        if (parkId is not null)
        {
            return await this.BuildParkChildNodesAsync(language, parkId, cancellationToken);
        }

        parkId = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.TryReadNodeValue(parentNodeId, "park-items");
        if (parkId is not null)
        {
            return await this.BuildParkItemNodesAsync(language, parkId, cancellationToken);
        }

        parkId = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.TryReadNodeValue(parentNodeId, "park-zones");
        if (parkId is not null)
        {
            return await this.BuildParkZoneNodesAsync(language, parkId, cancellationToken);
        }

        parkId = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.TryReadNodeValue(parentNodeId, "park-videos");
        if (parkId is not null)
        {
            return await this.BuildParkVideoNodesAsync(language, parkId, cancellationToken);
        }

        parkId = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.TryReadNodeValue(parentNodeId, "park-history");
        if (parkId is not null)
        {
            return await this.BuildParkHistoryArticleNodesAsync(language, parkId, cancellationToken);
        }

        string? itemId = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.TryReadNodeValue(parentNodeId, "park-item");
        if (itemId is not null)
        {
            return await this.BuildParkItemChildNodesAsync(language, itemId, cancellationToken);
        }

        itemId = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.TryReadNodeValue(parentNodeId, "park-item-videos");
        if (itemId is not null)
        {
            return await this.BuildParkItemVideoNodesAsync(language, itemId, cancellationToken);
        }

        itemId = GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.TryReadNodeValue(parentNodeId, "park-item-history");
        if (itemId is not null)
        {
            return await this.BuildParkItemHistoryArticleNodesAsync(language, itemId, cancellationToken);
        }

        return Array.Empty<PublicHtmlSitemapNode>();
    }

    internal static IReadOnlyCollection<PublicHtmlSitemapNode> BuildRootNodes(string language)
    {
        return new List<PublicHtmlSitemapNode>
        {
            GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf("home", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "home"), $"/{language}/home"),
            new PublicHtmlSitemapNode
            {
                Id = "parks",
                Label = GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "parks"),
                RelativeUrl = $"/{language}/parks",
                HasChildren = true
            },
            new PublicHtmlSitemapNode
            {
                Id = "technical",
                Label = GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "technical"),
                RelativeUrl = $"/{language}/technical",
                HasChildren = true
            },
            new PublicHtmlSitemapNode
            {
                Id = "references",
                Label = GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "references"),
                HasChildren = true
            },
            GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf("rankings", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "rankings"), $"/{language}/rankings"),
            GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf("rating-methodology", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "ratingMethodology"), $"/{language}/rankings/methodology"),
            GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf("about", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "about"), $"/{language}/about"),
            GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf("contact", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "contact"), $"/{language}/contact"),
            GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf("versions", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "versions"), $"/{language}/versions"),
            GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf("privacy", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "privacy"), $"/{language}/privacy"),
            GetPublicHtmlSitemapNodesQueryHandlerHelpersExtensions.CreateLeaf("sitemap", GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, "sitemap"), $"/{language}/sitemap"),
        };
    }

    internal static string NormalizeParentNodeId(string? parentNodeId)
    {
        return string.IsNullOrWhiteSpace(parentNodeId) ? "root" : parentNodeId.Trim();
    }
}
