using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.History;

public sealed class HistoryArticle
{
    public string? Slug { get; set; }

    public List<LocalizedText> Titles { get; set; } = new();

    public List<LocalizedText> Subtitles { get; set; } = new();

    public List<LocalizedText> Summaries { get; set; } = new();

    public string? MainImageId { get; set; }

    public List<HistoryArticleBlock> Blocks { get; set; } = new();

    public List<HistorySourceReference> Sources { get; set; } = new();

    public bool IsPublished { get; set; } = true;
}
