using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.TechnicalPages;

public sealed class TechnicalPageDto
{
    public string? Id { get; set; }

    public string CategoryKey { get; set; } = string.Empty;

    public List<LocalizedTextDto> CategoryNames { get; set; } = new();

    public string Slug { get; set; } = string.Empty;

    public List<LocalizedTextDto> Titles { get; set; } = new();

    public List<LocalizedTextDto> Summaries { get; set; } = new();

    public List<TechnicalPageAliasDto> Aliases { get; set; } = new();

    public List<TechnicalContentBlockDto> ContentBlocks { get; set; } = new();

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public AdminReviewStatusDto AdminReviewStatus { get; set; } = AdminReviewStatusDto.ToReview;

    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class TechnicalPagesJsonUpsertDto
{
    public List<TechnicalPageDto> Pages { get; set; } = new();
}

public sealed class TechnicalPagesJsonUpsertResultDto
{
    public int CreatedCount { get; set; }

    public int UpdatedCount { get; set; }

    public List<TechnicalPageDto> Pages { get; set; } = new();
}

public sealed class TechnicalPageAliasDto
{
    public string CategoryKey { get; set; } = string.Empty;

    public List<LocalizedTextDto> Labels { get; set; } = new();
}

public sealed class TechnicalContentBlockDto
{
    public string BlockType { get; set; } = "richText";

    public string? Tone { get; set; }

    public string? ImageUrl { get; set; }

    public string? ImageId { get; set; }

    public string? DiagramKey { get; set; }

    public List<LocalizedTextDto> Titles { get; set; } = new();

    public List<LocalizedTextDto> Bodies { get; set; } = new();

    public List<LocalizedTextDto> Captions { get; set; } = new();

    public List<LocalizedTextDto> AltTexts { get; set; } = new();

    public List<TechnicalContentListItemDto> Items { get; set; } = new();

    public TechnicalContentTableDto? Table { get; set; }

    public List<TechnicalContentMetricDto> Metrics { get; set; } = new();

    public List<TechnicalContentLinkDto> Links { get; set; } = new();

    public List<TechnicalContentBlockDto> Columns { get; set; } = new();
}

public sealed class TechnicalContentListItemDto
{
    public List<LocalizedTextDto> Texts { get; set; } = new();
}

public sealed class TechnicalContentTableDto
{
    public List<TechnicalContentTableCellDto> Headers { get; set; } = new();

    public List<TechnicalContentTableRowDto> Rows { get; set; } = new();
}

public sealed class TechnicalContentTableRowDto
{
    public List<TechnicalContentTableCellDto> Cells { get; set; } = new();
}

public sealed class TechnicalContentTableCellDto
{
    public List<LocalizedTextDto> Texts { get; set; } = new();
}

public sealed class TechnicalContentMetricDto
{
    public List<LocalizedTextDto> Label { get; set; } = new();

    public List<LocalizedTextDto> Value { get; set; } = new();

    public List<LocalizedTextDto> HelpText { get; set; } = new();
}

public sealed class TechnicalContentLinkDto
{
    public string Url { get; set; } = string.Empty;

    public List<LocalizedTextDto> Label { get; set; } = new();
}
