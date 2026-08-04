using System.Text.Json;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private static string? DescribeHistoryArticleForDiff(HistoryArticle? article)
    {
        if (article is null)
        {
            return null;
        }

        HistoryArticleComparisonSnapshot snapshot = new HistoryArticleComparisonSnapshot
        {
            Slug = NormalizeString(article.Slug),
            Titles = BuildLocalizedTextComparisonSnapshots(article.Titles),
            Subtitles = BuildLocalizedTextComparisonSnapshots(article.Subtitles),
            Summaries = BuildLocalizedTextComparisonSnapshots(article.Summaries),
            MainImageId = NormalizeString(article.MainImageId),
            Blocks = article.Blocks
                .OrderBy(static block => block.SortOrder)
                .ThenBy(static block => NormalizeString(block.Id), StringComparer.Ordinal)
                .Select(static block => BuildHistoryArticleBlockComparisonSnapshot(block))
                .ToList(),
            Sources = BuildHistorySourceComparisonSnapshots(article.Sources),
            IsPublished = article.IsPublished,
        };

        return JsonSerializer.Serialize(snapshot);
    }

    private static string DescribeHistorySourcesForDiff(IReadOnlyCollection<HistorySourceReference> sources)
    {
        return JsonSerializer.Serialize(BuildHistorySourceComparisonSnapshots(sources));
    }

    private static HistoryArticleBlockComparisonSnapshot BuildHistoryArticleBlockComparisonSnapshot(HistoryArticleBlock block)
    {
        return new HistoryArticleBlockComparisonSnapshot
        {
            Id = NormalizeString(block.Id),
            Type = block.Type,
            SortOrder = block.SortOrder,
            HeadingLevel = block.HeadingLevel,
            Texts = BuildLocalizedTextComparisonSnapshots(block.Texts),
            ImageId = NormalizeString(block.ImageId),
            ImageIds = block.ImageIds
                .Select(static imageId => NormalizeString(imageId))
                .Where(static imageId => imageId is not null)
                .Select(static imageId => imageId!)
                .Distinct(StringComparer.Ordinal)
                .ToList(),
            Captions = BuildLocalizedTextComparisonSnapshots(block.Captions),
        };
    }

    private static List<LocalizedTextComparisonSnapshot> BuildLocalizedTextComparisonSnapshots(IReadOnlyCollection<LocalizedText> texts)
    {
        Dictionary<string, string> values = ToLocalizedTextMap(texts);
        return values
            .OrderBy(static value => value.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static value => new LocalizedTextComparisonSnapshot
            {
                LanguageCode = value.Key,
                Value = value.Value,
            })
            .ToList();
    }

    private static List<HistorySourceComparisonSnapshot> BuildHistorySourceComparisonSnapshots(IReadOnlyCollection<HistorySourceReference> sources)
    {
        return sources
            .Select(static source => new HistorySourceComparisonSnapshot
            {
                Label = NormalizeString(source.Label),
                Url = NormalizeString(source.Url) ?? string.Empty,
                AccessedAt = NormalizeString(source.AccessedAt),
            })
            .OrderBy(static source => source.Url, StringComparer.Ordinal)
            .ThenBy(static source => source.Label, StringComparer.Ordinal)
            .ThenBy(static source => source.AccessedAt, StringComparer.Ordinal)
            .ToList();
    }

    private sealed class HistoryArticleComparisonSnapshot
    {
        public string? Slug { get; init; }

        public List<LocalizedTextComparisonSnapshot> Titles { get; init; } = new List<LocalizedTextComparisonSnapshot>();

        public List<LocalizedTextComparisonSnapshot> Subtitles { get; init; } = new List<LocalizedTextComparisonSnapshot>();

        public List<LocalizedTextComparisonSnapshot> Summaries { get; init; } = new List<LocalizedTextComparisonSnapshot>();

        public string? MainImageId { get; init; }

        public List<HistoryArticleBlockComparisonSnapshot> Blocks { get; init; } = new List<HistoryArticleBlockComparisonSnapshot>();

        public List<HistorySourceComparisonSnapshot> Sources { get; init; } = new List<HistorySourceComparisonSnapshot>();

        public bool IsPublished { get; init; }
    }

    private sealed class HistoryArticleBlockComparisonSnapshot
    {
        public string? Id { get; init; }

        public HistoryArticleBlockType Type { get; init; }

        public int SortOrder { get; init; }

        public int? HeadingLevel { get; init; }

        public List<LocalizedTextComparisonSnapshot> Texts { get; init; } = new List<LocalizedTextComparisonSnapshot>();

        public string? ImageId { get; init; }

        public List<string> ImageIds { get; init; } = new List<string>();

        public List<LocalizedTextComparisonSnapshot> Captions { get; init; } = new List<LocalizedTextComparisonSnapshot>();
    }

    private sealed class HistorySourceComparisonSnapshot
    {
        public string? Label { get; init; }

        public string Url { get; init; } = string.Empty;

        public string? AccessedAt { get; init; }
    }

    private sealed class LocalizedTextComparisonSnapshot
    {
        public string LanguageCode { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;
    }
}
