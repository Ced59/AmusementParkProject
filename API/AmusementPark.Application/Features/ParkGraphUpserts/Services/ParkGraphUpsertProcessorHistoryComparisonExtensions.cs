using System.Text.Json;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using System.Globalization;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Localization;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Core.Geo;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Parks.Services;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using System.Text;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;
internal static class ParkGraphUpsertProcessorHistoryComparisonExtensions
{
    internal static string? DescribeHistoryArticleForDiff(HistoryArticle? article)
    {
        if (article is null)
        {
            return null;
        }

        HistoryArticleComparisonSnapshot snapshot = new HistoryArticleComparisonSnapshot
        {
            Slug = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(article.Slug),
            Titles = ParkGraphUpsertProcessorHistoryComparisonExtensions.BuildLocalizedTextComparisonSnapshots(article.Titles),
            Subtitles = ParkGraphUpsertProcessorHistoryComparisonExtensions.BuildLocalizedTextComparisonSnapshots(article.Subtitles),
            Summaries = ParkGraphUpsertProcessorHistoryComparisonExtensions.BuildLocalizedTextComparisonSnapshots(article.Summaries),
            MainImageId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(article.MainImageId),
            Blocks = article.Blocks.OrderBy(static block => block.SortOrder).ThenBy(static block => ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(block.Id), StringComparer.Ordinal).Select(static block => ParkGraphUpsertProcessorHistoryComparisonExtensions.BuildHistoryArticleBlockComparisonSnapshot(block)).ToList(),
            Sources = ParkGraphUpsertProcessorHistoryComparisonExtensions.BuildHistorySourceComparisonSnapshots(article.Sources),
            IsPublished = article.IsPublished,
        };
        return JsonSerializer.Serialize(snapshot);
    }

    internal static string DescribeHistorySourcesForDiff(IReadOnlyCollection<HistorySourceReference> sources)
    {
        return JsonSerializer.Serialize(ParkGraphUpsertProcessorHistoryComparisonExtensions.BuildHistorySourceComparisonSnapshots(sources));
    }

    internal static HistoryArticleBlockComparisonSnapshot BuildHistoryArticleBlockComparisonSnapshot(HistoryArticleBlock block)
    {
        return new HistoryArticleBlockComparisonSnapshot
        {
            Id = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(block.Id),
            Type = block.Type,
            SortOrder = block.SortOrder,
            HeadingLevel = block.HeadingLevel,
            Texts = ParkGraphUpsertProcessorHistoryComparisonExtensions.BuildLocalizedTextComparisonSnapshots(block.Texts),
            ImageId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(block.ImageId),
            ImageIds = block.ImageIds.Select(static imageId => ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(imageId)).Where(static imageId => imageId is not null).Select(static imageId => imageId!).Distinct(StringComparer.Ordinal).ToList(),
            Captions = ParkGraphUpsertProcessorHistoryComparisonExtensions.BuildLocalizedTextComparisonSnapshots(block.Captions),
        };
    }

    internal static List<LocalizedTextComparisonSnapshot> BuildLocalizedTextComparisonSnapshots(IReadOnlyCollection<LocalizedText> texts)
    {
        Dictionary<string, string> values = ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextMap(texts);
        return values.OrderBy(static value => value.Key, StringComparer.OrdinalIgnoreCase).Select(static value => new LocalizedTextComparisonSnapshot { LanguageCode = value.Key, Value = value.Value, }).ToList();
    }

    internal static List<HistorySourceComparisonSnapshot> BuildHistorySourceComparisonSnapshots(IReadOnlyCollection<HistorySourceReference> sources)
    {
        return sources.Select(static source => new HistorySourceComparisonSnapshot { Label = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(source.Label), Url = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(source.Url) ?? string.Empty, AccessedAt = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(source.AccessedAt), }).OrderBy(static source => source.Url, StringComparer.Ordinal).ThenBy(static source => source.Label, StringComparer.Ordinal).ThenBy(static source => source.AccessedAt, StringComparer.Ordinal).ToList();
    }
}
