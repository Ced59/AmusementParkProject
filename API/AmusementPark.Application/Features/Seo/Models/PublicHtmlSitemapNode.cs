using System.Text.Json.Serialization;

namespace AmusementPark.Application.Features.Seo.Models;

public sealed class PublicHtmlSitemapNode
{
    public string Id { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string? RelativeUrl { get; init; }

    public bool HasChildren { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<PublicHtmlSitemapNode>? Children { get; init; }
}
