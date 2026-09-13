using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ProfileComparisonRepository : IProfileComparisonRepository
{
    private readonly IMongoCollection<ProfileComparisonDocument> collection;

    public ProfileComparisonRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal ProfileComparisonRepository(IMongoCollection<ProfileComparisonDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<ProfileComparison?> GetByIdAsync(
        ProfileComparisonId comparisonId,
        CancellationToken cancellationToken)
    {
        ProfileComparisonDocument? document = await this.collection
            .Find(ProfileComparisonMongoDefinitions.BuildIdFilter(comparisonId.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<ProfileComparison?> GetByShareTokenAsync(
        ShareToken shareToken,
        CancellationToken cancellationToken)
    {
        ProfileComparisonDocument? document = await this.collection
            .Find(ProfileComparisonMongoDefinitions.BuildShareTokenFilter(shareToken.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<ProfileComparison>> ListActiveByParticipantAsync(
        string userId,
        ProfileComparisonListCursor? after,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        List<ProfileComparisonDocument> documents = await this.collection
            .Find(ProfileComparisonMongoDefinitions.BuildActiveParticipantPageFilter(
                userId.Trim(),
                after))
            .SortByDescending(static document => document.CreatedAt)
            .ThenByDescending(static document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<ProfileComparisonWriteOutcome> CreateAsync(
        ProfileComparison comparison,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        try
        {
            await this.collection.InsertOneAsync(
                comparison.ToDocument(),
                cancellationToken: cancellationToken);
            return ProfileComparisonWriteOutcome.Success;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            ProfileComparisonDocument? existing = await this.collection
                .Find(ProfileComparisonMongoDefinitions.BuildIdFilter(comparison.Id.Value))
                .FirstOrDefaultAsync(cancellationToken);
            return existing is null
                ? ProfileComparisonWriteOutcome.TokenCollision
                : ProfileComparisonWriteOutcome.ConcurrencyConflict;
        }
    }

    public async Task<ProfileComparisonWriteOutcome> ReplaceAsync(
        ProfileComparison comparison,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        if (expectedVersion < 0
            || expectedVersion == long.MaxValue
            || comparison.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The comparison must be exactly one version ahead of the expected version.",
                nameof(comparison));
        }

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            ProfileComparisonMongoDefinitions.BuildVersionFilter(
                comparison.Id.Value,
                expectedVersion),
            comparison.ToDocument(),
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1
            ? ProfileComparisonWriteOutcome.Success
            : ProfileComparisonWriteOutcome.ConcurrencyConflict;
    }

    private static IMongoCollection<ProfileComparisonDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<ProfileComparisonDocument>(
            settings.ProfileComparisonsCollectionName);
    }
}
