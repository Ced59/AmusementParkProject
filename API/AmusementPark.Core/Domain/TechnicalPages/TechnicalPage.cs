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
