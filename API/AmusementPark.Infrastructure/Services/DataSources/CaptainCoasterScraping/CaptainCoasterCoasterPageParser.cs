using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoasterScraping;

internal interface ICaptainCoasterCoasterPageParser
{
    CaptainCoasterParsedCoaster Parse(CaptainCoasterDiscoveredUrl discoveredUrl, string html, CaptainCoasterScrapingSettings settings);
}

internal sealed partial class CaptainCoasterCoasterPageParser : ICaptainCoasterCoasterPageParser
{
    public CaptainCoasterParsedCoaster Parse(CaptainCoasterDiscoveredUrl discoveredUrl, string html, CaptainCoasterScrapingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(discoveredUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(html);
        ArgumentNullException.ThrowIfNull(settings);

        HtmlDocument document = new HtmlDocument();
        document.LoadHtml(html);

        HtmlNode? titleNode = document.DocumentNode.SelectSingleNode(settings.CoasterTitleXPath);
        string title = titleNode?.InnerText.CleanText() ?? discoveredUrl.Slug;
        (string coasterName, string? parkNameFromTitle) = ParseTitle(title, discoveredUrl.Slug);

        Dictionary<string, string> characteristics = this.ExtractCharacteristics(document, settings);
        List<string?> metricSlots = this.ExtractTopMetricSlots(document, settings);

        string? parkName = GetCharacteristic(characteristics, "Parc") ?? parkNameFromTitle;
        string? parkSlug = ExtractParkSlugFromUrl(discoveredUrl.Slug, coasterName, parkName);
        string? launchType = GetCharacteristic(characteristics, "Lancement");

        Dictionary<string, string> rawAttributes = new Dictionary<string, string>(characteristics, StringComparer.OrdinalIgnoreCase)
        {
            ["topMetrics"] = string.Join(" | ", metricSlots.Select(static item => string.IsNullOrWhiteSpace(item) ? "<empty>" : item))
        };

        return new CaptainCoasterParsedCoaster
        {
            ExternalId = discoveredUrl.CaptainCoasterId,
            Name = coasterName,
            Slug = discoveredUrl.Slug,
            SourceUrl = discoveredUrl.Url,
            ParkName = parkName,
            ParkSlug = parkSlug,
            CountryRaw = GetCharacteristic(characteristics, "Pays"),
            Manufacturer = NormalizeManufacturer(GetCharacteristic(characteristics, "Constructeur")),
            Model = GetCharacteristic(characteristics, "Modèle"),
            MaterialType = GetCharacteristic(characteristics, "Type"),
            SeatingType = GetCharacteristic(characteristics, "Train"),
            LaunchType = launchType,
            RestraintType = GetCharacteristic(characteristics, "Retenue"),
            IsLaunched = ComputeIsLaunched(launchType),
            HeightInMeters = ParseMetricDouble(metricSlots.ElementAtOrDefault(0)),
            SpeedInKmH = ParseMetricDouble(metricSlots.ElementAtOrDefault(1)),
            LengthInMeters = ParseMetricDouble(metricSlots.ElementAtOrDefault(2)),
            InversionCount = ParseMetricInteger(metricSlots.ElementAtOrDefault(3)),
            Status = GetCharacteristic(characteristics, "Statut"),
            OpeningDateText = GetCharacteristic(characteristics, "Ouverture"),
            ClosingDateText = GetCharacteristic(characteristics, "Fermeture"),
            ScrapedAtUtc = DateTime.UtcNow,
            RawAttributes = rawAttributes,
        };
    }

    private static (string CoasterName, string? ParkName) ParseTitle(string title, string fallbackSlug)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return (fallbackSlug, null);
        }

        string[] parts = title.Split('•', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            return (parts[0].CleanText(), parts[1].CleanText());
        }

        return (title.CleanText(), null);
    }

    private Dictionary<string, string> ExtractCharacteristics(HtmlDocument document, CaptainCoasterScrapingSettings settings)
    {
        Dictionary<string, string> characteristics = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        HtmlNodeCollection? nodes = document.DocumentNode.SelectNodes(settings.CharacteristicsItemXPath);
        if (nodes is null)
        {
            return characteristics;
        }

        foreach (HtmlNode itemNode in nodes)
        {
            HtmlNode? labelNode = itemNode.SelectSingleNode(settings.CharacteristicLabelXPath);
            if (labelNode is null)
            {
                continue;
            }

            string label = labelNode.InnerText.CleanText().TrimEnd(':').Trim();
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            string? value = itemNode.SelectSingleNode(settings.CharacteristicValueXPath)?.InnerText.CleanText();
            if (string.IsNullOrWhiteSpace(value))
            {
                string fullText = itemNode.InnerText.CleanText();
                if (fullText.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                {
                    fullText = fullText[label.Length..].TrimStart(':').Trim();
                }

                value = fullText;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                characteristics[label] = value.Trim();
            }
        }

        return characteristics;
    }

    private List<string?> ExtractTopMetricSlots(HtmlDocument document, CaptainCoasterScrapingSettings settings)
    {
        List<string?> values = document.DocumentNode
            .SelectNodes(settings.TopMetricXPath)?
            .Take(4)
            .Select(static node =>
            {
                string value = node.InnerText.CleanText();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            })
            .ToList()
            ?? new List<string?>();

        while (values.Count < 4)
        {
            values.Add(null);
        }

        return values;
    }

    private static string? GetCharacteristic(IReadOnlyDictionary<string, string> characteristics, string key)
    {
        if (characteristics.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return null;
    }

    private static double? ParseMetricDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        Match match = NumberRegex().Match(value);
        if (!match.Success)
        {
            return null;
        }

        string normalized = match.Groups["value"].Value.Replace(",", ".", StringComparison.Ordinal);
        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return parsed;
        }

        return null;
    }

    private static int? ParseMetricInteger(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        Match match = IntegerRegex().Match(value);
        if (!match.Success)
        {
            return null;
        }

        if (int.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool? ComputeIsLaunched(string? launchType)
    {
        if (string.IsNullOrWhiteSpace(launchType))
        {
            return null;
        }

        string normalized = launchType.ToLowerInvariant();
        if (normalized.Contains("propulsion", StringComparison.Ordinal) || normalized.Contains("launch", StringComparison.Ordinal))
        {
            return true;
        }

        if (normalized.Contains("lift", StringComparison.Ordinal) ||
            normalized.Contains("chaîne", StringComparison.Ordinal) ||
            normalized.Contains("chaine", StringComparison.Ordinal) ||
            normalized.Contains("câble", StringComparison.Ordinal) ||
            normalized.Contains("cable", StringComparison.Ordinal) ||
            normalized.Contains("treuil", StringComparison.Ordinal) ||
            normalized.Contains("pneus", StringComparison.Ordinal))
        {
            return false;
        }

        return null;
    }

    private static string? ExtractParkSlugFromUrl(string coasterSlug, string coasterName, string? parkName)
    {
        if (string.IsNullOrWhiteSpace(parkName))
        {
            return null;
        }

        string coasterNameSlug = coasterName.ToSlugValue();
        string parkNameSlug = parkName.ToSlugValue();

        if (coasterSlug.EndsWith("-" + parkNameSlug, StringComparison.OrdinalIgnoreCase))
        {
            return parkNameSlug;
        }

        if (coasterSlug.StartsWith(coasterNameSlug + "-", StringComparison.OrdinalIgnoreCase))
        {
            string remaining = coasterSlug[(coasterNameSlug.Length + 1)..];
            if (!string.IsNullOrWhiteSpace(remaining))
            {
                return remaining;
            }
        }

        return parkNameSlug;
    }

    private static string? NormalizeManufacturer(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        if (string.Equals(trimmed, "inconnu", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return trimmed;
    }

    [GeneratedRegex(@"(?<value>\d+(?:[\.,]\d+)?)", RegexOptions.Compiled)]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"(?<value>\d+)", RegexOptions.Compiled)]
    private static partial Regex IntegerRegex();
}
