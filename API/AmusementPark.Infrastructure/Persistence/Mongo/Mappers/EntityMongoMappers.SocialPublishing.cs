using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialPublishing;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static partial class EntityMongoMappers
{
    public static SocialPublication ToDomain(this SocialPublicationDocument document)
    {
        return new SocialPublication
        {
            Id = document.Id,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
            Network = ParseEnumOrDefault(document.Network, SocialNetwork.Facebook),
            Status = ParseEnumOrDefault(document.Status, SocialPublicationStatus.Pending),
            Trigger = ParseEnumOrDefault(document.Trigger, SocialPublicationTrigger.Manual),
            Message = document.Message,
            Url = document.Url,
            SourceEntityType = document.SourceEntityType,
            SourceEntityId = document.SourceEntityId,
            RequestedByUserId = document.RequestedByUserId,
            DeduplicationKey = document.DeduplicationKey,
            RequestedAtUtc = document.RequestedAtUtc,
            AttemptedAtUtc = document.AttemptedAtUtc,
            PublishedAtUtc = document.PublishedAtUtc,
            ExternalPostId = document.ExternalPostId,
            ExternalPostUrl = document.ExternalPostUrl,
            FailureCode = document.FailureCode,
            FailureMessage = document.FailureMessage,
        };
    }

    public static SocialPublicationDocument ToDocument(this SocialPublication entity)
    {
        return new SocialPublicationDocument
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
            Network = entity.Network.ToString(),
            Status = entity.Status.ToString(),
            Trigger = entity.Trigger.ToString(),
            Message = entity.Message,
            Url = entity.Url,
            SourceEntityType = entity.SourceEntityType,
            SourceEntityId = entity.SourceEntityId,
            RequestedByUserId = entity.RequestedByUserId,
            DeduplicationKey = entity.DeduplicationKey,
            RequestedAtUtc = entity.RequestedAtUtc,
            AttemptedAtUtc = entity.AttemptedAtUtc,
            PublishedAtUtc = entity.PublishedAtUtc,
            ExternalPostId = entity.ExternalPostId,
            ExternalPostUrl = entity.ExternalPostUrl,
            FailureCode = entity.FailureCode,
            FailureMessage = entity.FailureMessage,
        };
    }
}
