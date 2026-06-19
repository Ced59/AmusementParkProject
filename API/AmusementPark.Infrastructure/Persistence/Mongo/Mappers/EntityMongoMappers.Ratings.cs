using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static partial class EntityMongoMappers
{
    public static UserRating ToDomain(this UserRatingDocument document)
    {
        return new UserRating
        {
            Id = document.Id,
            UserId = document.UserId,
            TargetType = document.TargetType,
            TargetId = document.TargetId,
            ParkId = document.ParkId,
            ParkItemCategory = document.ParkItemCategory,
            ParkItemType = document.ParkItemType,
            Value = document.Value,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
        };
    }

    public static UserRatingDocument ToDocument(this UserRating entity)
    {
        return new UserRatingDocument
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? Guid.NewGuid().ToString("N") : entity.Id,
            UserId = entity.UserId,
            TargetType = entity.TargetType,
            TargetId = entity.TargetId,
            ParkId = entity.ParkId,
            ParkItemCategory = entity.ParkItemCategory,
            ParkItemType = entity.ParkItemType,
            Value = entity.Value,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static RatingAggregate ToDomain(this RatingAggregateDocument document)
    {
        return new RatingAggregate
        {
            Id = document.Id,
            TargetType = document.TargetType,
            TargetId = document.TargetId,
            ParkId = document.ParkId,
            ParkItemCategory = document.ParkItemCategory,
            ParkItemType = document.ParkItemType,
            RatingCount = document.RatingCount,
            RatingSum = document.RatingSum,
            AverageRating = document.AverageRating,
            BayesianScore = document.BayesianScore,
            LastRatedAtUtc = document.LastRatedAtUtc,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
        };
    }

    public static RatingAggregateDocument ToDocument(this RatingAggregate entity)
    {
        return new RatingAggregateDocument
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? Guid.NewGuid().ToString("N") : entity.Id,
            TargetType = entity.TargetType,
            TargetId = entity.TargetId,
            ParkId = entity.ParkId,
            ParkItemCategory = entity.ParkItemCategory,
            ParkItemType = entity.ParkItemType,
            RatingCount = entity.RatingCount,
            RatingSum = entity.RatingSum,
            AverageRating = entity.AverageRating,
            BayesianScore = entity.BayesianScore,
            LastRatedAtUtc = entity.LastRatedAtUtc,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }
}
