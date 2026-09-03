using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class UserVisitRepository : IUserVisitRepository
{
    public const int MaximumListSize = 100;

    private readonly IMongoCollection<UserVisitDocument> collection;

    public UserVisitRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);

        this.collection = database.GetCollection<UserVisitDocument>(
            settings.UserVisitsCollectionName);
    }

    public async Task<Visit> CreateAsync(
        Visit visit,
        CancellationToken cancellationToken)
    {
        UserVisitDocument document = visit.ToDocument();
        await this.collection.InsertOneAsync(
            document,
            cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<Visit?> GetOwnedAsync(
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        UserVisitDocument? document = await this.collection
            .Find(UserVisitMongoDefinitions.BuildOwnedVisitFilter(visitId.Value, userId))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<Visit>> ListOwnedAsync(
        string userId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > MaximumListSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                $"The visit list size must be between 1 and {MaximumListSize}.");
        }

        List<UserVisitDocument> documents = await this.collection
            .Find(UserVisitMongoDefinitions.BuildOwnerFilter(userId))
            .Sort(UserVisitMongoDefinitions.BuildNewestVisitSort())
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents
            .Select(static document => document.ToDomain())
            .ToList();
    }

    public async Task<bool> TryUpdateOwnedAsync(
        Visit visit,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        if (expectedVersion == long.MaxValue || visit.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The persisted visit must be exactly one version ahead of the expected version.",
                nameof(visit));
        }

        UserVisitDocument document = visit.ToDocument();
        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            UserVisitMongoDefinitions.BuildOwnedVersionFilter(
                document.Id,
                document.UserId,
                expectedVersion),
            document,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<bool> TryDeleteOwnedAsync(
        VisitId visitId,
        string userId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        DeleteResult result = await this.collection.DeleteOneAsync(
            UserVisitMongoDefinitions.BuildOwnedVersionFilter(
                visitId.Value,
                userId,
                expectedVersion),
            cancellationToken);
        return result.DeletedCount == 1;
    }
}
