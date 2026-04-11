using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;

internal interface ICaptainCoasterMapPageParser
{
    IReadOnlyCollection<CaptainCoasterParkCoordinate> Parse(string sourceUrl, string html, string markersAttributeName);
}

internal sealed partial class CaptainCoasterMapPageParser : ICaptainCoasterMapPageParser
{
    public IReadOnlyCollection<CaptainCoasterParkCoordinate> Parse(string sourceUrl, string html, string markersAttributeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);
        ArgumentException.ThrowIfNullOrWhiteSpace(markersAttributeName);

        string markersJson = ExtractMarkersJson(html, markersAttributeName);
        List<RawParkMapMarker>? rawMarkers = JsonSerializer.Deserialize<List<RawParkMapMarker>>(markersJson, SerializerOptions);
        if (rawMarkers is null || rawMarkers.Count == 0)
        {
            throw new InvalidOperationException("Aucun marqueur de parc n'a pu être désérialisé depuis la page carte.");
        }

        List<CaptainCoasterParkCoordinate> coordinates = rawMarkers
            .Where(static marker => !string.IsNullOrWhiteSpace(marker.Name))
            .Select(marker => new CaptainCoasterParkCoordinate
            {
                MapParkId = GetJsonElementAsString(marker.Id),
                Name = marker.Name!.Trim(),
                Latitude = marker.Latitude,
                Longitude = marker.Longitude,
                CoasterCount = marker.Nb,
                SourceUrl = sourceUrl,
                ExtractedAtUtc = DateTime.UtcNow,
            })
            .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return coordinates;
    }

    private static string ExtractMarkersJson(string html, string markersAttributeName)
    {
        HtmlDocument document = new HtmlDocument();
        document.LoadHtml(html);

        HtmlNode? nodeWithAttribute = document.DocumentNode
            .Descendants()
            .FirstOrDefault(node => node.Attributes[markersAttributeName] is not null);

        string? jsonFromAttribute = nodeWithAttribute?.GetAttributeValue(markersAttributeName, null);
        if (!string.IsNullOrWhiteSpace(jsonFromAttribute))
        {
            return HtmlEntity.DeEntitize(jsonFromAttribute).Trim();
        }

        string encodedAttributeName = Regex.Escape(markersAttributeName);
        Regex regex = new Regex(encodedAttributeName + "\\s*=\\s*(?:\"(?<json>\\[[^\"]*\\])\"|'(?<json>\\[[^']*\\])')", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        Match regexMatch = regex.Match(html);
        if (regexMatch.Success)
        {
            return HtmlEntity.DeEntitize(regexMatch.Groups["json"].Value).Trim();
        }

        throw new InvalidOperationException($"Impossible de trouver l'attribut {markersAttributeName} dans le HTML de la page carte.");
    }

    private static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private static string? GetJsonElementAsString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => value.GetRawText(),
        };
    }

    private sealed class RawParkMapMarker
    {
        public JsonElement Id { get; init; }

        public string? Name { get; init; }

        public double? Latitude { get; init; }

        public double? Longitude { get; init; }

        public int? Nb { get; init; }
    }
}
