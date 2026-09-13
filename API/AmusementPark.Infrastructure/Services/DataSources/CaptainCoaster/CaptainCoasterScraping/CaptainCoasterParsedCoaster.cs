using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;

internal sealed class CaptainCoasterParsedCoaster
{
    public string ExternalId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string SourceUrl { get; init; } = string.Empty;

    public string? ParkName { get; init; }

    public string? ParkSlug { get; init; }

    public string? CountryRaw { get; init; }

    public string? Manufacturer { get; init; }

    public string? Model { get; init; }

    public string? MaterialType { get; init; }

    public string? SeatingType { get; init; }

    public string? LaunchType { get; init; }

    public string? RestraintType { get; init; }

    public bool? IsLaunched { get; init; }

    public double? HeightInMeters { get; init; }

    public double? LengthInMeters { get; init; }

    public double? SpeedInKmH { get; init; }

    public int? InversionCount { get; init; }

    public string? Status { get; init; }

    public string? OpeningDateText { get; init; }

    public string? ClosingDateText { get; init; }

    public DateTime ScrapedAtUtc { get; init; }

    public IReadOnlyDictionary<string, string> RawAttributes { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
