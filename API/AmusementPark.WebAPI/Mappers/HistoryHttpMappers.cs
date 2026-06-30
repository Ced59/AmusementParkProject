using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.History;

namespace AmusementPark.WebAPI.Mappers;

internal static class HistoryHttpMappers
{
    public static HistoryTimelineDto ToHttp(this HistoryTimelineResult result)
    {
        return new HistoryTimelineDto
        {
            EntityType = result.EntityType.ToString(),
            Park = result.Park?.ToHttp(),
            ParkItem = result.ParkItem?.ToHttp(),
            IncludedParkItems = result.IncludedParkItems.Select(static item => item.ToHttp()).ToList(),
            Events = result.Events.Select(static item => item.ToHttp()).ToList(),
        };
    }

    public static HistoryTimelineEventDto ToHttp(this HistoryTimelineEventResult result)
    {
        return new HistoryTimelineEventDto
        {
            Event = result.Event.ToHttp(),
            ContextPark = result.ContextPark?.ToHttp(),
            ParkItem = result.ParkItem?.ToHttp(),
            MainImage = result.MainImage?.ToHttp(),
        };
    }

    public static HistoryArticleDto ToHttp(this HistoryArticleResult result)
    {
        return new HistoryArticleDto
        {
            Event = result.Event.ToHttp(),
            Park = result.Park?.ToHttp(),
            ParkItem = result.ParkItem?.ToHttp(),
            ContextPark = result.ContextPark?.ToHttp(),
            MainImage = result.MainImage?.ToHttp(),
        };
    }

    public static HistoryEventDto ToHttp(this HistoryEvent historyEvent)
    {
        return new HistoryEventDto
        {
            Id = historyEvent.Id,
            Key = historyEvent.Key,
            EntityType = historyEvent.EntityType.ToString(),
            OwnerId = historyEvent.OwnerId,
            ParkId = historyEvent.ParkId,
            ParkItemId = historyEvent.ParkItemId,
            ContextParkId = historyEvent.ContextParkId,
            Year = historyEvent.Year,
            Month = historyEvent.Month,
            Day = historyEvent.Day,
            DatePrecision = historyEvent.DatePrecision.ToString(),
            EventType = historyEvent.EventType,
            IsMajor = historyEvent.IsMajor,
            IsVisible = historyEvent.IsVisible,
            Slug = historyEvent.Slug,
            Titles = historyEvent.Titles.ToHttp(),
            Summaries = historyEvent.Summaries.ToHttp(),
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
            Sources = historyEvent.Sources.Select(static source => source.ToHttp()).ToList(),
            Article = historyEvent.Article?.ToHttp(),
            CreatedAtUtc = historyEvent.CreatedAtUtc,
            UpdatedAtUtc = historyEvent.UpdatedAtUtc,
        };
    }

    public static HistoryEventWriteModel ToApplication(this HistoryEventDto dto)
    {
        HistoryEntityType entityType = ParseEnum(dto.EntityType, HistoryEntityType.Park);
        return new HistoryEventWriteModel
        {
            Id = Normalize(dto.Id),
            Key = Normalize(dto.Key),
            EntityType = entityType,
            OwnerId = Normalize(dto.OwnerId),
            ParkId = Normalize(dto.ParkId),
            ParkItemId = Normalize(dto.ParkItemId),
            ContextParkId = Normalize(dto.ContextParkId),
            Year = dto.Year,
            Month = dto.Month,
            Day = dto.Day,
            DatePrecision = ParseEnum(dto.DatePrecision, HistoryDatePrecision.Year),
            EventType = Normalize(dto.EventType) ?? string.Empty,
            IsMajor = dto.IsMajor,
            IsVisible = dto.IsVisible,
            Slug = Normalize(dto.Slug),
            Titles = dto.Titles.ToDomain(),
            Summaries = dto.Summaries.ToDomain(),
            MainImageId = Normalize(dto.MainImageId),
            PreviousName = Normalize(dto.PreviousName),
            NewName = Normalize(dto.NewName),
            PreviousLogoImageId = Normalize(dto.PreviousLogoImageId),
            NewLogoImageId = Normalize(dto.NewLogoImageId),
            PreviousOperatorId = Normalize(dto.PreviousOperatorId),
            NewOperatorId = Normalize(dto.NewOperatorId),
            LocationLabel = Normalize(dto.LocationLabel),
            RelatedParkIds = dto.RelatedParkIds,
            RelatedParkItemIds = dto.RelatedParkItemIds,
            Sources = dto.Sources.Select(static source => source.ToDomain()).ToList(),
            Article = dto.Article?.ToDomain(),
        };
    }

    public static PagedResponseDto<HistoryTimelineEventDto> ToHistoryPagedHttp(this PagedResult<HistoryTimelineEventResult> page)
    {
        return page.ToPagedResponse(static item => item.ToHttp());
    }

    private static HistorySourceReferenceDto ToHttp(this HistorySourceReference source)
    {
        return new HistorySourceReferenceDto
        {
            Label = source.Label,
            Url = source.Url,
            AccessedAt = source.AccessedAt,
        };
    }

    private static HistorySourceReference ToDomain(this HistorySourceReferenceDto dto)
    {
        return new HistorySourceReference
        {
            Label = Normalize(dto.Label),
            Url = Normalize(dto.Url) ?? string.Empty,
            AccessedAt = Normalize(dto.AccessedAt),
        };
    }

    private static HistoryArticleContentDto ToHttp(this HistoryArticle article)
    {
        return new HistoryArticleContentDto
        {
            Slug = article.Slug,
            Titles = article.Titles.ToHttp(),
            Subtitles = article.Subtitles.ToHttp(),
            Summaries = article.Summaries.ToHttp(),
            MainImageId = article.MainImageId,
            Blocks = article.Blocks.OrderBy(static block => block.SortOrder).Select(static block => block.ToHttp()).ToList(),
            Sources = article.Sources.Select(static source => source.ToHttp()).ToList(),
            IsPublished = article.IsPublished,
        };
    }

    private static HistoryArticle ToDomain(this HistoryArticleContentDto dto)
    {
        return new HistoryArticle
        {
            Slug = Normalize(dto.Slug),
            Titles = dto.Titles.ToDomain(),
            Subtitles = dto.Subtitles.ToDomain(),
            Summaries = dto.Summaries.ToDomain(),
            MainImageId = Normalize(dto.MainImageId),
            Blocks = dto.Blocks.Select(static block => block.ToDomain()).ToList(),
            Sources = dto.Sources.Select(static source => source.ToDomain()).ToList(),
            IsPublished = dto.IsPublished,
        };
    }

    private static HistoryArticleBlockDto ToHttp(this HistoryArticleBlock block)
    {
        return new HistoryArticleBlockDto
        {
            Id = block.Id,
            Type = block.Type.ToString(),
            SortOrder = block.SortOrder,
            HeadingLevel = block.HeadingLevel,
            Texts = block.Texts.ToHttp(),
            ImageId = block.ImageId,
            ImageIds = block.ImageIds.ToList(),
            Captions = block.Captions.ToHttp(),
        };
    }

    private static HistoryArticleBlock ToDomain(this HistoryArticleBlockDto dto)
    {
        return new HistoryArticleBlock
        {
            Id = Normalize(dto.Id) ?? Guid.NewGuid().ToString("N"),
            Type = ParseEnum(dto.Type, HistoryArticleBlockType.Paragraph),
            SortOrder = dto.SortOrder,
            HeadingLevel = dto.HeadingLevel,
            Texts = dto.Texts.ToDomain(),
            ImageId = Normalize(dto.ImageId),
            ImageIds = dto.ImageIds.Where(static id => !string.IsNullOrWhiteSpace(id)).Select(static id => id.Trim()).Distinct(StringComparer.Ordinal).ToList(),
            Captions = dto.Captions.ToDomain(),
        };
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return Enum.TryParse(value, true, out TEnum parsed) ? parsed : fallback;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
