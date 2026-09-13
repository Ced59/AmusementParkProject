using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;

internal interface ICaptainCoasterMapPageParser
{
    IReadOnlyCollection<CaptainCoasterParkCoordinate> Parse(string sourceUrl, string html, string markersAttributeName);
}
