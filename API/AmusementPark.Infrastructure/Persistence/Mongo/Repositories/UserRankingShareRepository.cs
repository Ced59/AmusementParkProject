using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class UserRankingShareRepository : IUserRankingShareRepository
{
    private readonly IMongoCollection<UserRankingShareDocument> collection;

    public UserRankingShareRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<UserRankingShareDocument>(
            settings.UserRankingSharesCollectionName);
    }

    public async Task<UserRankingShare?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        UserRankingShareDocument? document = await this.collection
            .Find(item => item.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<UserRankingShare?> GetPublicByShareIdAsync(
        string shareId,
        CancellationToken cancellationToken)
    {
        UserRankingShareDocument? document = await this.collection
            .Find(item => item.IsPublic && item.ShareId == shareId)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<UserRankingShare> UpsertAsync(
        UserRankingShare share,
        CancellationToken cancellationToken)
    {
        UserRankingShareDocument document = share.ToDocument();
        await this.collection.ReplaceOneAsync(
            item => item.UserId == document.UserId,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
        return document.ToDomain();
    }
}
