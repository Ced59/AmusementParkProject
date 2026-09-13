using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;

internal sealed class CaptainCoasterDiscoveredUrl
{
    public string Url { get; init; } = string.Empty;

    public string Language { get; init; } = "fr";

    public string CaptainCoasterId { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;
}
