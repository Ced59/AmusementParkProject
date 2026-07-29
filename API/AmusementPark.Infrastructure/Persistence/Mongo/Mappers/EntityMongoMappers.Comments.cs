using AmusementPark.Core.Domain.Comments;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static partial class EntityMongoMappers
{
    public static Comment ToDomain(this CommentDocument document)
    {
        return new Comment
        {
            Id = document.Id,
            TargetType = document.TargetType,
            TargetId = document.TargetId,
            ParkId = document.ParkId,
            AuthorUserId = document.AuthorUserId,
            AuthorDisplayName = string.IsNullOrWhiteSpace(document.AuthorDisplayName)
                ? "User"
                : document.AuthorDisplayName,
            AuthorAvatarUrl = document.AuthorAvatarUrl,
            AuthorRole = document.AuthorRole,
            Bodies = CommonMongoMappers.ToDomain(document.Bodies),
            ImageIds = document.ImageIds,
            IsOfficial = document.IsOfficial,
            ModerationStatus = document.ModerationStatus,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
        };
    }

    public static CommentDocument ToDocument(this Comment entity)
    {
        return new CommentDocument
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? Guid.NewGuid().ToString("N") : entity.Id,
            TargetType = entity.TargetType,
            TargetId = entity.TargetId,
            ParkId = entity.ParkId,
            AuthorUserId = entity.AuthorUserId,
            AuthorDisplayName = entity.AuthorDisplayName,
            AuthorAvatarUrl = entity.AuthorAvatarUrl,
            AuthorRole = entity.AuthorRole,
            Bodies = CommonMongoMappers.ToDocuments(entity.Bodies),
            ImageIds = entity.ImageIds,
            IsOfficial = entity.IsOfficial,
            ModerationStatus = entity.ModerationStatus,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }
}
