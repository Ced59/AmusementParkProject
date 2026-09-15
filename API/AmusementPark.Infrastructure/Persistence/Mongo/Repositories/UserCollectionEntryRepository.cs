using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class UserCollectionEntryRepository : IUserCollectionEntryRepository
{
    private readonly IMongoCollection<UserCollectionEntryDocument> collection;

    public UserCollectionEntryRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal UserCollectionEntryRepository(
        IMongoCollection<UserCollectionEntryDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<IReadOnlyCollection<UserCollectionEntry>> ListOwnedAsync(
        string userId,
        CollectionTargetType? targetType,
        string? targetId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinition<UserCollectionEntryDocument> filter =
            UserCollectionEntryMongoDefinitions.BuildOwnerFilter(normalizedUserId);
        if (targetType.HasValue)
        {
            filter &= Builders<UserCollectionEntryDocument>.Filter.Eq(
                static document => document.TargetType,
                targetType.Value);
        }

        if (!string.IsNullOrWhiteSpace(targetId))
        {
            filter &= Builders<UserCollectionEntryDocument>.Filter.Eq(
                static document => document.TargetId,
                targetId.Trim());
        }

        List<UserCollectionEntryDocument> documents = await this.collection
            .Find(filter)
            .SortByDescending(static document => document.UpdatedAt)
            .ThenBy(static document => document.Id)
            .Limit(UserCollectionEntry.MaximumEntriesPerUser)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<UserCollectionEntry?> GetOwnedByIdentityAsync(
        string userId,
        CollectionTargetType targetType,
        string targetId,
        UserCollectionKind kind,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string normalizedTargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        UserCollectionEntryDocument? document = await this.collection.Find(
            UserCollectionEntryMongoDefinitions.BuildIdentityFilter(
                normalizedUserId,
                targetType,
                normalizedTargetId,
                kind)).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<UserCollectionWriteOutcome> CreateAsync(
        UserCollectionEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        List<UserCollectionEntryDocument> ownedDocuments = await this.collection
            .Find(UserCollectionEntryMongoDefinitions.BuildOwnerFilter(entry.UserId))
            .Limit(UserCollectionEntry.MaximumEntriesPerUser)
            .ToListAsync(cancellationToken);
        if (ownedDocuments.Any(document =>
            document.TargetType == entry.TargetType
            && string.Equals(document.TargetId, entry.TargetId, StringComparison.Ordinal)
            && document.Kind == entry.Kind))
        {
            return UserCollectionWriteOutcome.AlreadyExists;
        }

        HashSet<int> occupiedSlots = ownedDocuments
            .Select(static document => document.OwnerSlot)
            .Where(static slot => slot >= 0)
            .ToHashSet();
        for (int ownerSlot = 0; ownerSlot < UserCollectionEntry.MaximumEntriesPerUser; ownerSlot++)
        {
            if (occupiedSlots.Contains(ownerSlot))
            {
                continue;
            }

            UserCollectionEntryDocument document = entry.ToDocument();
            document.OwnerSlot = ownerSlot;
            try
            {
                await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
                return UserCollectionWriteOutcome.Success;
            }
            catch (MongoWriteException exception)
                when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                bool identityExists = await this.collection.Find(
                    UserCollectionEntryMongoDefinitions.BuildIdentityFilter(
                        entry.UserId,
                        entry.TargetType,
                        entry.TargetId,
                        entry.Kind)).Limit(1).AnyAsync(cancellationToken);
                if (identityExists)
                {
                    return UserCollectionWriteOutcome.AlreadyExists;
                }
            }
        }

        return UserCollectionWriteOutcome.LimitReached;
    }

    public async Task<bool> TrySynchronizeTargetStatusesAsync(
        IReadOnlyCollection<UserCollectionEntry> entries,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
        {
            return true;
        }

        List<WriteModel<UserCollectionEntryDocument>> writes = entries.Select(entry =>
        {
            FilterDefinition<UserCollectionEntryDocument> filter =
                Builders<UserCollectionEntryDocument>.Filter.Eq(
                    static document => document.Id,
                    entry.Id.Value)
                & Builders<UserCollectionEntryDocument>.Filter.Eq(
                    static document => document.UserId,
                    entry.UserId)
                & Builders<UserCollectionEntryDocument>.Filter.Eq(
                    static document => document.Version,
                    entry.Version - 1);
            UpdateDefinition<UserCollectionEntryDocument> update =
                Builders<UserCollectionEntryDocument>.Update
                    .Set(static document => document.TargetStatus, entry.TargetStatus)
                    .Set(static document => document.UpdatedAt, entry.UpdatedAtUtc)
                    .Set(static document => document.Version, entry.Version);
            return new UpdateOneModel<UserCollectionEntryDocument>(filter, update);
        }).Cast<WriteModel<UserCollectionEntryDocument>>().ToList();
        BulkWriteResult<UserCollectionEntryDocument> result = await this.collection.BulkWriteAsync(
            writes,
            new BulkWriteOptions { IsOrdered = false },
            cancellationToken);
        return result.MatchedCount == entries.Count;
    }

    public async Task DeleteOwnedByIdentityAsync(
        string userId,
        CollectionTargetType targetType,
        string targetId,
        UserCollectionKind kind,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string normalizedTargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        await this.collection.DeleteOneAsync(
            UserCollectionEntryMongoDefinitions.BuildIdentityFilter(
                normalizedUserId,
                targetType,
                normalizedTargetId,
                kind),
            cancellationToken);
    }

    private static IMongoCollection<UserCollectionEntryDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<UserCollectionEntryDocument>(
            settings.UserCollectionEntriesCollectionName);
    }
}
