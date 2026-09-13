using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;


namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;

internal interface ICaptainCoasterCoasterPageParser
{
    CaptainCoasterParsedCoaster Parse(CaptainCoasterDiscoveredUrl discoveredUrl, string html, CaptainCoasterScrapingSettings settings);
}
