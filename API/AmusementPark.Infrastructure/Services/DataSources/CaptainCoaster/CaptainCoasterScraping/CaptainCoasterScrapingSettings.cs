using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;

internal sealed class CaptainCoasterScrapingSettings
{
    public string SitemapUrl { get; init; } = "https://captaincoaster.com/sitemap.xml";

    public string MapPageUrl { get; init; } = "https://captaincoaster.com/fr/map/";

    public string Language { get; init; } = "fr";

    public int DelayBetweenRequestsMs { get; init; } = 1200;

    public int TimeoutSeconds { get; init; } = 30;

    public int MaxRetryCount { get; init; } = 3;

    public int MaxConcurrentRequests { get; init; } = 4;

    public int CoasterWriteBatchSize { get; init; } = 50;

    public int ProgressSaveInterval { get; init; } = 25;

    public int? MaxCoasterCount { get; init; }

    public int SkipCoasterCount { get; init; }

    public bool EnrichParkCoordinates { get; init; } = true;

    public string MapMarkersAttributeName { get; init; } = "data-map-markers-value";

    public string CoasterTitleXPath { get; init; } = "//h1";

    public string CharacteristicsItemXPath { get; init; } = "//div[contains(@class,'list-group-item')]";

    public string CharacteristicLabelXPath { get; init; } = ".//label";

    public string CharacteristicValueXPath { get; init; } = ".//div[contains(@class,'pull-right')]";

    public string TopMetricXPath { get; init; } = "//button[contains(@class,'btn-float-lg')]//div[contains(@class,'text-bold')]";
}
