using AmusementPark.Core.Domain.SocialShare;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialShare;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static partial class EntityMongoMappers
{
    public static SocialShareEvent ToDomain(this SocialShareEventDocument document)
    {
        SocialShareEvent entity = new SocialShareEvent
        {
            Id = document.Id,
            OccurredAtUtc = document.OccurredAtUtc,
            TargetType = ParseEnumOrDefault(document.TargetType, SocialShareTargetType.Page),
            TargetId = document.TargetId,
            TargetTitle = document.TargetTitle,
            Url = document.Url,
            LanguageCode = document.LanguageCode,
            Channel = ParseEnumOrDefault(document.Channel, SocialShareChannel.Copy),
            VisitorKind = ParseEnumOrDefault(document.VisitorKind, SocialShareVisitorKind.Anonymous),
            UserId = document.UserId,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static SocialShareEventDocument ToDocument(this SocialShareEvent entity)
    {
        return new SocialShareEventDocument
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
            OccurredAtUtc = entity.OccurredAtUtc,
            TargetType = entity.TargetType.ToString(),
            TargetId = entity.TargetId,
            TargetTitle = entity.TargetTitle,
            Url = entity.Url,
            LanguageCode = entity.LanguageCode,
            Channel = entity.Channel.ToString(),
            VisitorKind = entity.VisitorKind.ToString(),
            UserId = entity.UserId,
        };
    }

    private static TEnum ParseEnumOrDefault<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value, true, out TEnum parsed) ? parsed : fallback;
    }
}
