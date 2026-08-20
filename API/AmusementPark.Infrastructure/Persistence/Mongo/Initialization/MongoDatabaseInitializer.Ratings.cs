using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeRatingsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<UserRatingDocument> userRatingsCollection = this.database.GetCollection<UserRatingDocument>(this.settings.UserRatingsCollectionName);
        List<CreateIndexModel<UserRatingDocument>> userRatingIndexes = new List<CreateIndexModel<UserRatingDocument>>
        {
            new CreateIndexModel<UserRatingDocument>(
                Builders<UserRatingDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.TargetType)
                    .Ascending(static document => document.TargetId),
                new CreateIndexOptions { Name = "idx_user_ratings_user_target_unique", Unique = true }),
            new CreateIndexModel<UserRatingDocument>(
                Builders<UserRatingDocument>.IndexKeys
                    .Ascending(static document => document.TargetType)
                    .Ascending(static document => document.TargetId),
                new CreateIndexOptions { Name = "idx_user_ratings_target" }),
            new CreateIndexModel<UserRatingDocument>(
                Builders<UserRatingDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Descending(static document => document.UpdatedAt),
                new CreateIndexOptions { Name = "idx_user_ratings_user_updated" }),
            new CreateIndexModel<UserRatingDocument>(
                Builders<UserRatingDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.ParkId),
                new CreateIndexOptions { Name = "idx_user_ratings_user_park" }),
        };

        await userRatingsCollection.Indexes.CreateManyAsync(userRatingIndexes, cancellationToken: cancellationToken);

        IMongoCollection<RatingAggregateDocument> ratingAggregatesCollection = this.database.GetCollection<RatingAggregateDocument>(this.settings.RatingAggregatesCollectionName);
        List<CreateIndexModel<RatingAggregateDocument>> ratingAggregateIndexes = new List<CreateIndexModel<RatingAggregateDocument>>
        {
            new CreateIndexModel<RatingAggregateDocument>(
                Builders<RatingAggregateDocument>.IndexKeys
                    .Ascending(static document => document.TargetType)
                    .Ascending(static document => document.TargetId),
                new CreateIndexOptions { Name = "idx_rating_aggregates_target_unique", Unique = true }),
            new CreateIndexModel<RatingAggregateDocument>(
                Builders<RatingAggregateDocument>.IndexKeys
                    .Descending(static document => document.BayesianScore)
                    .Descending(static document => document.RatingCount)
                    .Descending(static document => document.AverageRating),
                new CreateIndexOptions { Name = "idx_rating_aggregates_ranking" }),
            new CreateIndexModel<RatingAggregateDocument>(
                Builders<RatingAggregateDocument>.IndexKeys
                    .Ascending(static document => document.TargetType)
                    .Descending(static document => document.BayesianScore)
                    .Descending(static document => document.RatingCount),
                new CreateIndexOptions { Name = "idx_rating_aggregates_type_ranking" }),
            new CreateIndexModel<RatingAggregateDocument>(
                Builders<RatingAggregateDocument>.IndexKeys
                    .Ascending(static document => document.ParkItemCategory)
                    .Descending(static document => document.BayesianScore)
                    .Descending(static document => document.RatingCount),
                new CreateIndexOptions { Name = "idx_rating_aggregates_category_ranking" }),
        };

        await ratingAggregatesCollection.Indexes.CreateManyAsync(ratingAggregateIndexes, cancellationToken: cancellationToken);

        IMongoCollection<UserRankingShareDocument> sharesCollection = this.database.GetCollection<UserRankingShareDocument>(
            this.settings.UserRankingSharesCollectionName);
        List<CreateIndexModel<UserRankingShareDocument>> shareIndexes = new List<CreateIndexModel<UserRankingShareDocument>>
        {
            new CreateIndexModel<UserRankingShareDocument>(
                Builders<UserRankingShareDocument>.IndexKeys.Ascending(static document => document.UserId),
                new CreateIndexOptions { Name = "idx_user_ranking_shares_user_unique", Unique = true }),
            new CreateIndexModel<UserRankingShareDocument>(
                Builders<UserRankingShareDocument>.IndexKeys.Ascending(static document => document.ShareId),
                new CreateIndexOptions<UserRankingShareDocument>
                {
                    Name = "idx_user_ranking_shares_share_id_unique",
                    Unique = true,
                    PartialFilterExpression = Builders<UserRankingShareDocument>.Filter.And(
                        Builders<UserRankingShareDocument>.Filter.Eq(static document => document.IsPublic, true),
                        Builders<UserRankingShareDocument>.Filter.Type(static document => document.ShareId, BsonType.String)),
                }),
        };

        await sharesCollection.Indexes.CreateManyAsync(shareIndexes, cancellationToken: cancellationToken);
    }
}
