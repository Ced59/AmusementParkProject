using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.TechnicalPages;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.TechnicalPages.Results;

public sealed class TechnicalPageResult
{
    public string? Id { get; init; }

    public string CategoryKey { get; init; } = string.Empty;

    public IReadOnlyCollection<LocalizedText> CategoryNames { get; init; } = Array.Empty<LocalizedText>();

    public string Slug { get; init; } = string.Empty;

    public IReadOnlyCollection<LocalizedText> Titles { get; init; } = Array.Empty<LocalizedText>();

    public IReadOnlyCollection<LocalizedText> Summaries { get; init; } = Array.Empty<LocalizedText>();

    public IReadOnlyCollection<TechnicalPageAlias> Aliases { get; init; } = Array.Empty<TechnicalPageAlias>();

    public IReadOnlyCollection<TechnicalContentBlock> ContentBlocks { get; init; } = Array.Empty<TechnicalContentBlock>();

    public int SortOrder { get; init; }

    public bool IsVisible { get; init; }

    public AdminReviewStatus AdminReviewStatus { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public static TechnicalPageResult FromDomain(TechnicalPage page)
    {
        return new TechnicalPageResult
        {
            Id = page.Id,
            CategoryKey = page.CategoryKey,
            CategoryNames = page.CategoryNames,
            Slug = page.Slug,
            Titles = page.Titles,
            Summaries = page.Summaries,
            Aliases = page.Aliases,
            ContentBlocks = page.ContentBlocks,
            SortOrder = page.SortOrder,
            IsVisible = page.IsVisible,
            AdminReviewStatus = page.AdminReviewStatus,
            UpdatedAtUtc = page.UpdatedAtUtc,
        };
    }
}

public sealed class TechnicalPageJsonUpsertResult
{
    public int CreatedCount { get; init; }

    public int UpdatedCount { get; init; }

    public IReadOnlyCollection<TechnicalPageResult> Pages { get; init; } = Array.Empty<TechnicalPageResult>();
}
