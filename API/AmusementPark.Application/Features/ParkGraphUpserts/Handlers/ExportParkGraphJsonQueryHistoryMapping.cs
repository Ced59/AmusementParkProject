using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;

public sealed partial class ExportParkGraphJsonQueryHandler
{
    private static ParkGraphExportHistory MapHistory(IReadOnlyCollection<HistoryEvent> historyEvents)
    {
        return new ParkGraphExportHistory
        {
            Events = historyEvents
                .Select(static historyEvent => MapHistoryEvent(historyEvent))
                .ToList(),
        };
    }

    private static ParkGraphExportHistoryEvent MapHistoryEvent(HistoryEvent historyEvent)
    {
        bool isParkItemEvent = historyEvent.EntityType == HistoryEntityType.ParkItem;
        string? parkItemKey = isParkItemEvent ? historyEvent.OwnerId : null;

        return new ParkGraphExportHistoryEvent
        {
            Key = historyEvent.Key,
            EntityType = historyEvent.EntityType,
            Owner = isParkItemEvent ? "parkItem" : "park",
            OwnerId = historyEvent.OwnerId,
            ParkId = historyEvent.ParkId,
            ParkItemId = historyEvent.ParkItemId,
            ItemKey = parkItemKey,
            ParkItemKey = parkItemKey,
            ContextParkId = historyEvent.ContextParkId,
            Year = historyEvent.Year,
            Month = historyEvent.Month,
            Day = historyEvent.Day,
            DatePrecision = historyEvent.DatePrecision,
            EventType = historyEvent.EventType,
            IsMajor = historyEvent.IsMajor,
            IsVisible = historyEvent.IsVisible,
            Slug = historyEvent.Slug,
            Titles = CopyLocalizedTexts(historyEvent.Titles),
            Summaries = CopyLocalizedTexts(historyEvent.Summaries),
            MainImageId = historyEvent.MainImageId,
            PreviousName = historyEvent.PreviousName,
            NewName = historyEvent.NewName,
            PreviousLogoImageId = historyEvent.PreviousLogoImageId,
            NewLogoImageId = historyEvent.NewLogoImageId,
            PreviousOperatorId = historyEvent.PreviousOperatorId,
            NewOperatorId = historyEvent.NewOperatorId,
            LocationLabel = historyEvent.LocationLabel,
            RelatedParkIds = historyEvent.RelatedParkIds.ToList(),
            RelatedParkItemIds = historyEvent.RelatedParkItemIds.ToList(),
            Sources = historyEvent.Sources.Select(static source => MapHistorySource(source)).ToList(),
            Article = historyEvent.Article is null ? null : MapHistoryArticle(historyEvent.Article),
        };
    }

    private static ParkGraphExportHistoryArticle MapHistoryArticle(HistoryArticle article)
    {
        return new ParkGraphExportHistoryArticle
        {
            Slug = article.Slug,
            Titles = CopyLocalizedTexts(article.Titles),
            Subtitles = CopyLocalizedTexts(article.Subtitles),
            Summaries = CopyLocalizedTexts(article.Summaries),
            MainImageId = article.MainImageId,
            Blocks = article.Blocks
                .OrderBy(static block => block.SortOrder)
                .Select(static block => MapHistoryArticleBlock(block))
                .ToList(),
            Sources = article.Sources.Select(static source => MapHistorySource(source)).ToList(),
            IsPublished = article.IsPublished,
        };
    }

    private static ParkGraphExportHistoryArticleBlock MapHistoryArticleBlock(HistoryArticleBlock block)
    {
        return new ParkGraphExportHistoryArticleBlock
        {
            Id = block.Id,
            Type = block.Type,
            SortOrder = block.SortOrder,
            HeadingLevel = block.HeadingLevel,
            Texts = CopyLocalizedTexts(block.Texts),
            ImageId = block.ImageId,
            ImageIds = block.ImageIds.ToList(),
            Captions = CopyLocalizedTexts(block.Captions),
        };
    }

    private static ParkGraphExportHistorySource MapHistorySource(HistorySourceReference source)
    {
        return new ParkGraphExportHistorySource
        {
            Label = source.Label,
            Url = source.Url,
            AccessedAt = source.AccessedAt,
        };
    }
}
