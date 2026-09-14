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

    public Task<long> CountOwnedAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(
            ownerUserId,
            nameof(ownerUserId));
        return this.collection.CountDocumentsAsync(
            Builders<ParkFitGroupProfileDocument>.Filter.Eq(
                static document => document.OwnerUserId,
                normalizedOwnerUserId),
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyCollection<ParkFitGroupProfile>> ListOwnedAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(
            ownerUserId,
            nameof(ownerUserId));
        List<ParkFitGroupProfileDocument> documents = await this.collection
            .Find(Builders<ParkFitGroupProfileDocument>.Filter.Eq(
                static document => document.OwnerUserId,
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
        try
        {
            await this.collection.InsertOneAsync(
                profile.ToDocument(),
                cancellationToken: cancellationToken);
            return ParkFitGroupProfileWriteOutcome.Success;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ParkFitGroupProfileWriteOutcome.AliasConflict;
        }
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

        try
        {
            ReplaceOneResult result = await this.collection.ReplaceOneAsync(
                ParkFitGroupProfileMongoDefinitions.BuildOwnedVersionFilter(
                    profile.Id.Value,
                    profile.OwnerUserId,
                    expectedVersion),
                profile.ToDocument(),
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
