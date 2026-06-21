using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.TechnicalPages;

/// <summary>
/// Public technical explanation page for attraction components and coaster systems.
/// </summary>
public sealed class TechnicalPage : AuditableEntity
{
    public string CategoryKey { get; set; } = string.Empty;

    public List<LocalizedText> CategoryNames { get; set; } = new();

    public string Slug { get; set; } = string.Empty;

    public List<LocalizedText> Titles { get; set; } = new();

    public List<LocalizedText> Summaries { get; set; } = new();

    public List<TechnicalPageAlias> Aliases { get; set; } = new();

    public List<TechnicalContentBlock> ContentBlocks { get; set; } = new();

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;
}

public sealed class TechnicalPageAlias
{
    public string CategoryKey { get; set; } = string.Empty;

    public List<LocalizedText> Labels { get; set; } = new();
}

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

public sealed class TechnicalContentListItem
{
    public List<LocalizedText> Texts { get; set; } = new();
}

public sealed class TechnicalContentTable
{
    public List<TechnicalContentTableCell> Headers { get; set; } = new();

    public List<TechnicalContentTableRow> Rows { get; set; } = new();
}

public sealed class TechnicalContentTableRow
{
    public List<TechnicalContentTableCell> Cells { get; set; } = new();
}

public sealed class TechnicalContentTableCell
{
    public List<LocalizedText> Texts { get; set; } = new();
}

public sealed class TechnicalContentMetric
{
    public List<LocalizedText> Label { get; set; } = new();

    public List<LocalizedText> Value { get; set; } = new();

    public List<LocalizedText> HelpText { get; set; } = new();
}

public sealed class TechnicalContentLink
{
    public string Url { get; set; } = string.Empty;

    public List<LocalizedText> Label { get; set; } = new();
}
