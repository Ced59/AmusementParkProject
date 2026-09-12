namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// Clés stables des sections de sitemap exposées publiquement.
/// </summary>
public static class SitemapSectionKeys
{
    public const string Static = "static";
    public const string Parks = "parks";
    public const string ParkOpeningHours = "park-opening-hours";
    public const string ParkPricing = "park-pricing";
    public const string History = "history";
    public const string HistoryArticles = "history-articles";
    public const string ParkImages = "park-images";
    public const string ParkVideos = "park-videos";
    public const string ParkItemLists = "park-item-lists";
    public const string ParkZones = "park-zones";
    public const string ParkItems = "park-items";
    public const string StandaloneAttractions = "standalone-attractions";
    public const string ParkItemImages = "park-item-images";
    public const string ParkItemVideos = "park-item-videos";
    public const string References = "references";
    public const string TechnicalPages = "technical-pages";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Static,
        Parks,
        ParkOpeningHours,
        ParkPricing,
        History,
        HistoryArticles,
        ParkImages,
        ParkVideos,
        ParkItemLists,
        ParkZones,
        ParkItems,
        StandaloneAttractions,
        ParkItemImages,
        ParkItemVideos,
        References,
        TechnicalPages,
    };
}
