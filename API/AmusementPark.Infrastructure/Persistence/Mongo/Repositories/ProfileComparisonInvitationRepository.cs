using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ProfileComparisonInvitationRepository
    : IProfileComparisonInvitationRepository
{
    private readonly IMongoCollection<ProfileComparisonInvitationDocument> collection;

    public ProfileComparisonInvitationRepository(
        IMongoDatabase database,
        MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal ProfileComparisonInvitationRepository(
        IMongoCollection<ProfileComparisonInvitationDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<ProfileComparisonInvitation?> GetByTokenAsync(
        ShareToken token,
        CancellationToken cancellationToken)
    {
        ProfileComparisonInvitationDocument? document = await this.collection
            .Find(ProfileComparisonInvitationMongoDefinitions.BuildTokenFilter(token.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<ProfileComparisonInvitationWriteOutcome> CreateAsync(
        ProfileComparisonInvitation invitation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        try
        {
            await this.collection.InsertOneAsync(
                invitation.ToDocument(),
                cancellationToken: cancellationToken);
            return ProfileComparisonInvitationWriteOutcome.Success;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ProfileComparisonInvitationWriteOutcome.TokenCollision;
        }
    }

    public async Task<ProfileComparisonInvitationWriteOutcome> ReplaceAsync(
        ProfileComparisonInvitation invitation,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        if (expectedVersion < 0
            || expectedVersion == long.MaxValue
            || invitation.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The invitation must be exactly one version ahead of the expected version.",
                nameof(invitation));
        }

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            ProfileComparisonInvitationMongoDefinitions.BuildVersionFilter(
                invitation.Id.Value,
                expectedVersion),
            invitation.ToDocument(),
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1
            ? ProfileComparisonInvitationWriteOutcome.Success
            : ProfileComparisonInvitationWriteOutcome.ConcurrencyConflict;
    }

    private static IMongoCollection<ProfileComparisonInvitationDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<ProfileComparisonInvitationDocument>(
            settings.ProfileComparisonInvitationsCollectionName);
    }
}
