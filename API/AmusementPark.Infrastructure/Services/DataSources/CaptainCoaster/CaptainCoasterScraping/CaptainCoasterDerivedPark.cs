using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;

internal sealed class CaptainCoasterDerivedPark
{
    public string ExternalId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Slug { get; init; }

    public string? SourceUrl { get; init; }

    public string? CountryRaw { get; init; }

    public int CoasterCount { get; init; }

    public IReadOnlyCollection<string> SampleCoasterNames { get; init; } = Array.Empty<string>();

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public string? MapParkId { get; init; }

    public string? CoordinateSourceUrl { get; init; }

    public DateTime ScrapedAtUtc { get; init; }
}
