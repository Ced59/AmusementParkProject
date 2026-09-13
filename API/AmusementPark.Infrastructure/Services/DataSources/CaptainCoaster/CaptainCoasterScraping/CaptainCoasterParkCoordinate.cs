using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;

internal sealed class CaptainCoasterParkCoordinate
{
    public string? MapParkId { get; init; }

    public string Name { get; init; } = string.Empty;

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public int? CoasterCount { get; init; }

    public string SourceUrl { get; init; } = string.Empty;

    public DateTime ExtractedAtUtc { get; init; }
}
