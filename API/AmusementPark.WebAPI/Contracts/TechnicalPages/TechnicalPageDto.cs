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
