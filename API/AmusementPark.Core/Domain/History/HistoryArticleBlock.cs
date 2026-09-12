using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.History;

public sealed class HistoryArticleBlock
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public HistoryArticleBlockType Type { get; set; } = HistoryArticleBlockType.Paragraph;

    public int SortOrder { get; set; }

    public int? HeadingLevel { get; set; }

    public List<LocalizedText> Texts { get; set; } = new();

    public string? ImageId { get; set; }

    public List<string> ImageIds { get; set; } = new();

    public List<LocalizedText> Captions { get; set; } = new();
}
