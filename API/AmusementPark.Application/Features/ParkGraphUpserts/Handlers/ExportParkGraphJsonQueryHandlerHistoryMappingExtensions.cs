using System.Globalization;
using System.Text;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkPricing.Ports;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Application.Common.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
internal static class ExportParkGraphJsonQueryHandlerHistoryMappingExtensions
{
    internal static ParkGraphExportHistory MapHistory(IReadOnlyCollection<HistoryEvent> historyEvents)
    {
        return new ParkGraphExportHistory
        {
            Events = historyEvents.Select(static historyEvent => ExportParkGraphJsonQueryHandlerHistoryMappingExtensions.MapHistoryEvent(historyEvent)).ToList(),
        };
    }

    internal static ParkGraphExportHistoryEvent MapHistoryEvent(HistoryEvent historyEvent)
    {
        bool isParkItemEvent = historyEvent.EntityType == HistoryEntityType.ParkItem;
        string? parkItemKey = isParkItemEvent ? historyEvent.OwnerId : null;
        string owner = historyEvent.EntityType switch
        {
            HistoryEntityType.ParkItem => "parkItem",
            HistoryEntityType.StandaloneAttraction => "standaloneAttraction",
            _ => "park",
        };
        return new ParkGraphExportHistoryEvent
        {
            Key = historyEvent.Key,
            EntityType = historyEvent.EntityType,
            Owner = owner,
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
            Titles = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(historyEvent.Titles),
            Summaries = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(historyEvent.Summaries),
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
            Sources = historyEvent.Sources.Select(static source => ExportParkGraphJsonQueryHandlerHistoryMappingExtensions.MapHistorySource(source)).ToList(),
            Article = historyEvent.Article is null ? null : ExportParkGraphJsonQueryHandlerHistoryMappingExtensions.MapHistoryArticle(historyEvent.Article),
        };
    }

    internal static ParkGraphExportHistoryArticle MapHistoryArticle(HistoryArticle article)
    {
        return new ParkGraphExportHistoryArticle
        {
            Slug = article.Slug,
            Titles = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(article.Titles),
            Subtitles = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(article.Subtitles),
            Summaries = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(article.Summaries),
            MainImageId = article.MainImageId,
            Blocks = article.Blocks.OrderBy(static block => block.SortOrder).Select(static block => ExportParkGraphJsonQueryHandlerHistoryMappingExtensions.MapHistoryArticleBlock(block)).ToList(),
            Sources = article.Sources.Select(static source => ExportParkGraphJsonQueryHandlerHistoryMappingExtensions.MapHistorySource(source)).ToList(),
            IsPublished = article.IsPublished,
        };
    }

    internal static ParkGraphExportHistoryArticleBlock MapHistoryArticleBlock(HistoryArticleBlock block)
    {
        return new ParkGraphExportHistoryArticleBlock
        {
            Id = block.Id,
            Type = block.Type,
            SortOrder = block.SortOrder,
            HeadingLevel = block.HeadingLevel,
            Texts = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(block.Texts),
            ImageId = block.ImageId,
            ImageIds = block.ImageIds.ToList(),
            Captions = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(block.Captions),
        };
    }

    internal static ParkGraphExportHistorySource MapHistorySource(HistorySourceReference source)
    {
        return new ParkGraphExportHistorySource
        {
            Label = source.Label,
            Url = source.Url,
            AccessedAt = source.AccessedAt,
        };
    }
}
