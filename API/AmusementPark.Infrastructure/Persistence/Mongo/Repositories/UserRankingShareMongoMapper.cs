using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class UserRankingShareMongoMapper
{
    public static UserRankingShareDocument ToDocument(this UserRankingShare share)
    {
        return new UserRankingShareDocument
        {
            Id = share.Id,
            UserId = share.UserId,
            IsPublic = share.IsPublic,
            ShareId = share.ShareId,
            PublishedAtUtc = share.PublishedAtUtc,
            CreatedAt = share.CreatedAtUtc,
            UpdatedAt = share.UpdatedAtUtc,
        };
    }

    public static UserRankingShare ToDomain(this UserRankingShareDocument document)
    {
        return UserRankingShare.Restore(
            document.Id,
            document.UserId,
            document.IsPublic,
            document.ShareId,
            document.PublishedAtUtc,
            document.CreatedAt,
            document.UpdatedAt);
    }
}
