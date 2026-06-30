using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Contracts.Parks;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoryTimelineDto
{
    public string EntityType { get; set; } = string.Empty;

    public ParkDto? Park { get; set; }

    public ParkItemDto? ParkItem { get; set; }

    public List<ParkItemDto> IncludedParkItems { get; set; } = new();

    public List<HistoryTimelineEventDto> Events { get; set; } = new();
}

public sealed class HistoryTimelineEventDto
{
    public HistoryEventDto Event { get; set; } = new HistoryEventDto();

    public ParkDto? ContextPark { get; set; }

    public ParkItemDto? ParkItem { get; set; }

    public ImageDto? MainImage { get; set; }
}

public sealed class HistoryArticleDto
{
    public HistoryEventDto Event { get; set; } = new HistoryEventDto();

    public ParkDto? Park { get; set; }

    public ParkItemDto? ParkItem { get; set; }

    public ParkDto? ContextPark { get; set; }

    public ImageDto? MainImage { get; set; }
}

public sealed class HistoryEventDto
{
    public string? Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string OwnerId { get; set; } = string.Empty;

    public string? ParkId { get; set; }

    public string? ParkItemId { get; set; }

    public string? ContextParkId { get; set; }

    public int Year { get; set; }

    public int? Month { get; set; }

    public int? Day { get; set; }

    public string DatePrecision { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public bool IsMajor { get; set; }

    public bool IsVisible { get; set; } = true;

    public string? Slug { get; set; }

    public List<LocalizedTextDto> Titles { get; set; } = new();

    public List<LocalizedTextDto> Summaries { get; set; } = new();

    public string? MainImageId { get; set; }

    public string? PreviousName { get; set; }

    public string? NewName { get; set; }

    public string? PreviousLogoImageId { get; set; }

    public string? NewLogoImageId { get; set; }

    public string? PreviousOperatorId { get; set; }

    public string? NewOperatorId { get; set; }

    public string? LocationLabel { get; set; }

    public List<string> RelatedParkIds { get; set; } = new();

    public List<string> RelatedParkItemIds { get; set; } = new();

    public List<HistorySourceReferenceDto> Sources { get; set; } = new();

    public HistoryArticleContentDto? Article { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class HistorySourceReferenceDto
{
    public string? Label { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? AccessedAt { get; set; }
}

public sealed class HistoryArticleContentDto
{
    public string? Slug { get; set; }

    public List<LocalizedTextDto> Titles { get; set; } = new();

    public List<LocalizedTextDto> Subtitles { get; set; } = new();

    public List<LocalizedTextDto> Summaries { get; set; } = new();

    public string? MainImageId { get; set; }

    public List<HistoryArticleBlockDto> Blocks { get; set; } = new();

    public List<HistorySourceReferenceDto> Sources { get; set; } = new();

    public bool IsPublished { get; set; } = true;
}

public sealed class HistoryArticleBlockDto
{
    public string? Id { get; set; }

    public string Type { get; set; } = "Paragraph";

    public int SortOrder { get; set; }

    public int? HeadingLevel { get; set; }

    public List<LocalizedTextDto> Texts { get; set; } = new();

    public string? ImageId { get; set; }

    public List<string> ImageIds { get; set; } = new();

    public List<LocalizedTextDto> Captions { get; set; } = new();
}
