using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.TechnicalPages;

public sealed class TechnicalContentBlock
{
    public string BlockType { get; set; } = "richText";

    public string? Tone { get; set; }

    public string? ImageUrl { get; set; }

    public string? ImageId { get; set; }

    public string? DiagramKey { get; set; }

    public List<LocalizedText> Titles { get; set; } = new();

    public List<LocalizedText> Bodies { get; set; } = new();

    public List<LocalizedText> Captions { get; set; } = new();

    public List<LocalizedText> AltTexts { get; set; } = new();

    public List<TechnicalContentListItem> Items { get; set; } = new();

    public TechnicalContentTable? Table { get; set; }

    public List<TechnicalContentMetric> Metrics { get; set; } = new();

    public List<TechnicalContentLink> Links { get; set; } = new();

    public List<TechnicalContentBlock> Columns { get; set; } = new();
}
