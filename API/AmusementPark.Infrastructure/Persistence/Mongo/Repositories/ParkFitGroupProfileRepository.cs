using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ParkFitGroupProfileRepository : IParkFitGroupProfileRepository
{
    private readonly IMongoCollection<ParkFitGroupProfileDocument> collection;

    public ParkFitGroupProfileRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal ParkFitGroupProfileRepository(
        IMongoCollection<ParkFitGroupProfileDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<IReadOnlyCollection<ParkFitGroupProfile>> ListOwnedAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(
            ownerUserId,
            nameof(ownerUserId));
        List<ParkFitGroupProfileDocument> documents = await this.collection
            .Find(ParkFitGroupProfileMongoDefinitions.BuildOwnerFilter(
                normalizedOwnerUserId))
            .SortByDescending(static document => document.UpdatedAt)
            .ThenBy(static document => document.Id)
            .Limit(ParkFitGroupProfile.MaximumProfilesPerOwner)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<ParkFitGroupProfile?> GetOwnedAsync(
        ParkFitGroupProfileId profileId,
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(
            ownerUserId,
            nameof(ownerUserId));
        ParkFitGroupProfileDocument? document = await this.collection
            .Find(ParkFitGroupProfileMongoDefinitions.BuildOwnedIdFilter(
                profileId.Value,
                normalizedOwnerUserId))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<ParkFitGroupProfileWriteOutcome> CreateAsync(
        ParkFitGroupProfile profile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        List<ParkFitGroupProfileDocument> ownedDocuments = await this.collection
            .Find(ParkFitGroupProfileMongoDefinitions.BuildOwnerFilter(profile.OwnerUserId))
            .Limit(ParkFitGroupProfile.MaximumProfilesPerOwner)
            .ToListAsync(cancellationToken);
        if (ownedDocuments.Any(document => string.Equals(
            document.NormalizedAlias,
            profile.NormalizedAlias,
            StringComparison.Ordinal)))
        {
            return ParkFitGroupProfileWriteOutcome.AliasConflict;
        }

        HashSet<int> occupiedSlots = ownedDocuments
            .Select(static document => document.OwnerSlot)
            .Where(static ownerSlot => ownerSlot >= 0)
            .ToHashSet();
        for (int ownerSlot = 0;
            ownerSlot < ParkFitGroupProfile.MaximumProfilesPerOwner;
            ownerSlot++)
        {
            if (occupiedSlots.Contains(ownerSlot))
            {
                continue;
            }

            ParkFitGroupProfileDocument document = profile.ToDocument();
            document.OwnerSlot = ownerSlot;
            try
            {
                await this.collection.InsertOneAsync(
                    document,
                    cancellationToken: cancellationToken);
                return ParkFitGroupProfileWriteOutcome.Success;
            }
            catch (MongoWriteException exception)
                when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                bool aliasAlreadyExists = await this.collection
                    .Find(ParkFitGroupProfileMongoDefinitions.BuildOwnedAliasFilter(
                        profile.OwnerUserId,
                        profile.NormalizedAlias))
                    .Limit(1)
                    .AnyAsync(cancellationToken);
                if (aliasAlreadyExists)
                {
                    return ParkFitGroupProfileWriteOutcome.AliasConflict;
                }
            }
        }

        return ParkFitGroupProfileWriteOutcome.LimitReached;
    }

    public async Task<ParkFitGroupProfileWriteOutcome> ReplaceAsync(
        ParkFitGroupProfile profile,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (expectedVersion < 1
            || expectedVersion == long.MaxValue
            || profile.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The profile must be exactly one version ahead of the expected version.",
                nameof(profile));
        }

        ParkFitGroupProfileDocument? current = await this.collection
            .Find(ParkFitGroupProfileMongoDefinitions.BuildOwnedVersionFilter(
                profile.Id.Value,
                profile.OwnerUserId,
                expectedVersion))
            .FirstOrDefaultAsync(cancellationToken);
        if (current is null)
        {
            return ParkFitGroupProfileWriteOutcome.ConcurrencyConflict;
        }

        try
        {
            ParkFitGroupProfileDocument replacement = profile.ToDocument();
            replacement.OwnerSlot = current.OwnerSlot;
            ReplaceOneResult result = await this.collection.ReplaceOneAsync(
                ParkFitGroupProfileMongoDefinitions.BuildOwnedVersionFilter(
                    profile.Id.Value,
                    profile.OwnerUserId,
                    expectedVersion),
                replacement,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken);
            return result.MatchedCount == 1
                ? ParkFitGroupProfileWriteOutcome.Success
                : ParkFitGroupProfileWriteOutcome.ConcurrencyConflict;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ParkFitGroupProfileWriteOutcome.AliasConflict;
        }
    }

    public async Task<ParkFitGroupProfileWriteOutcome> DeleteOwnedAsync(
        ParkFitGroupProfileId profileId,
        string ownerUserId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(
            ownerUserId,
            nameof(ownerUserId));
        if (expectedVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedVersion));
        }

        DeleteResult result = await this.collection.DeleteOneAsync(
            ParkFitGroupProfileMongoDefinitions.BuildOwnedVersionFilter(
                profileId.Value,
                normalizedOwnerUserId,
                expectedVersion),
            cancellationToken);
        return result.DeletedCount == 1
            ? ParkFitGroupProfileWriteOutcome.Success
            : ParkFitGroupProfileWriteOutcome.ConcurrencyConflict;
    }

    private static IMongoCollection<ParkFitGroupProfileDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<ParkFitGroupProfileDocument>(
            settings.UserGroupProfilesCollectionName);
    }
}
