using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static partial class EntityMongoMappers
{
    public static HistoryEvent ToDomain(this HistoryEventDocument document)
    {
        HistoryEvent entity = new HistoryEvent
        {
            Id = document.Id,
            Key = document.Key,
            EntityType = document.EntityType,
            OwnerId = document.OwnerId,
            ParkId = document.ParkId,
            ParkItemId = document.ParkItemId,
            ContextParkId = document.ContextParkId,
            Year = document.Year,
            Month = document.Month,
            Day = document.Day,
            DatePrecision = document.DatePrecision,
            EventType = document.EventType,
            IsMajor = document.IsMajor,
            IsVisible = document.IsVisible,
            Slug = document.Slug,
            Titles = CommonMongoMappers.ToDomain(document.Titles),
            Summaries = CommonMongoMappers.ToDomain(document.Summaries),
            MainImageId = document.MainImageId,
            PreviousName = document.PreviousName,
            NewName = document.NewName,
            PreviousLogoImageId = document.PreviousLogoImageId,
            NewLogoImageId = document.NewLogoImageId,
            PreviousOperatorId = document.PreviousOperatorId,
            NewOperatorId = document.NewOperatorId,
            LocationLabel = document.LocationLabel,
            RelatedParkIds = document.RelatedParkIds.ToList(),
            RelatedParkItemIds = document.RelatedParkItemIds.ToList(),
            Sources = document.Sources.Select(ToDomain).ToList(),
            Article = document.Article?.ToDomain(),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static HistoryEventDocument ToDocument(this HistoryEvent entity)
    {
        return new HistoryEventDocument
        {
            Id = entity.Id,
            Key = entity.Key,
            EntityType = entity.EntityType,
            OwnerId = entity.OwnerId,
            ParkId = entity.ParkId,
            ParkItemId = entity.ParkItemId,
            ContextParkId = entity.ContextParkId,
            Year = entity.Year,
            Month = entity.Month,
            Day = entity.Day,
            DatePrecision = entity.DatePrecision,
            EventType = entity.EventType,
            IsMajor = entity.IsMajor,
            IsVisible = entity.IsVisible,
            Slug = entity.Slug,
            Titles = CommonMongoMappers.ToDocuments(entity.Titles),
            Summaries = CommonMongoMappers.ToDocuments(entity.Summaries),
            MainImageId = entity.MainImageId,
            PreviousName = entity.PreviousName,
            NewName = entity.NewName,
            PreviousLogoImageId = entity.PreviousLogoImageId,
            NewLogoImageId = entity.NewLogoImageId,
            PreviousOperatorId = entity.PreviousOperatorId,
            NewOperatorId = entity.NewOperatorId,
            LocationLabel = entity.LocationLabel,
            RelatedParkIds = entity.RelatedParkIds.ToList(),
            RelatedParkItemIds = entity.RelatedParkItemIds.ToList(),
            Sources = entity.Sources.Select(ToDocument).ToList(),
            Article = entity.Article?.ToDocument(),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    private static HistorySourceReference ToDomain(this HistorySourceReferenceDocument document)
    {
        return new HistorySourceReference
        {
            Label = document.Label,
            Url = document.Url,
            AccessedAt = document.AccessedAt,
        };
    }

    private static HistorySourceReferenceDocument ToDocument(this HistorySourceReference entity)
    {
        return new HistorySourceReferenceDocument
        {
            Label = entity.Label,
            Url = entity.Url,
            AccessedAt = entity.AccessedAt,
        };
    }

    private static HistoryArticle ToDomain(this HistoryArticleDocument document)
    {
        return new HistoryArticle
        {
            Slug = document.Slug,
            Titles = CommonMongoMappers.ToDomain(document.Titles),
            Subtitles = CommonMongoMappers.ToDomain(document.Subtitles),
            Summaries = CommonMongoMappers.ToDomain(document.Summaries),
            MainImageId = document.MainImageId,
            Blocks = document.Blocks.Select(ToDomain).ToList(),
            Sources = document.Sources.Select(ToDomain).ToList(),
            IsPublished = document.IsPublished,
        };
    }

    private static HistoryArticleDocument ToDocument(this HistoryArticle entity)
    {
        return new HistoryArticleDocument
        {
            Slug = entity.Slug,
            Titles = CommonMongoMappers.ToDocuments(entity.Titles),
            Subtitles = CommonMongoMappers.ToDocuments(entity.Subtitles),
            Summaries = CommonMongoMappers.ToDocuments(entity.Summaries),
            MainImageId = entity.MainImageId,
            Blocks = entity.Blocks.Select(ToDocument).ToList(),
            Sources = entity.Sources.Select(ToDocument).ToList(),
            IsPublished = entity.IsPublished,
        };
    }

    private static HistoryArticleBlock ToDomain(this HistoryArticleBlockDocument document)
    {
        return new HistoryArticleBlock
        {
            Id = document.Id,
            Type = document.Type,
            SortOrder = document.SortOrder,
            HeadingLevel = document.HeadingLevel,
            Texts = CommonMongoMappers.ToDomain(document.Texts),
            ImageId = document.ImageId,
            ImageIds = document.ImageIds.ToList(),
            Captions = CommonMongoMappers.ToDomain(document.Captions),
        };
    }

    private static HistoryArticleBlockDocument ToDocument(this HistoryArticleBlock entity)
    {
        return new HistoryArticleBlockDocument
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? Guid.NewGuid().ToString("N") : entity.Id,
            Type = entity.Type,
            SortOrder = entity.SortOrder,
            HeadingLevel = entity.HeadingLevel,
            Texts = CommonMongoMappers.ToDocuments(entity.Texts),
            ImageId = entity.ImageId,
            ImageIds = entity.ImageIds.ToList(),
            Captions = CommonMongoMappers.ToDocuments(entity.Captions),
        };
    }
}
