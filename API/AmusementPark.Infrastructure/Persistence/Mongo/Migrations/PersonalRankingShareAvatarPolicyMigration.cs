using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

/// <summary>
/// Retire de façon idempotente l'avatar des politiques de classement migrées avant
/// que leur projection publique ne sache réellement le servir.
/// </summary>
public sealed class PersonalRankingShareAvatarPolicyMigration
{
    private readonly IMongoCollection<SharePublicationDocument> collection;
    private readonly TimeProvider timeProvider;

    public PersonalRankingShareAvatarPolicyMigration(
        IMongoDatabase database,
        MongoDbSettings settings)
        : this(
            database.GetCollection<SharePublicationDocument>(
                settings.SharePublicationsCollectionName),
            TimeProvider.System)
    {
    }

    internal PersonalRankingShareAvatarPolicyMigration(
        IMongoCollection<SharePublicationDocument> collection,
        TimeProvider timeProvider)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        await this.collection.UpdateManyAsync(
            BuildDraftPolicyFilter(),
            BuildDraftCorrectionUpdate(nowUtc),
            cancellationToken: cancellationToken);
        await this.collection.UpdateManyAsync(
            BuildVersionedPolicyFilter(),
            BuildVersionedCorrectionUpdate(nowUtc),
            cancellationToken: cancellationToken);
    }

    internal static FilterDefinition<SharePublicationDocument> BuildDraftPolicyFilter()
    {
        return BuildAffectedPolicyFilter()
            & Builders<SharePublicationDocument>.Filter.Eq(
                publication => publication.Status,
                SharePublicationStatus.Draft);
    }

    internal static FilterDefinition<SharePublicationDocument> BuildVersionedPolicyFilter()
    {
        return BuildAffectedPolicyFilter()
            & Builders<SharePublicationDocument>.Filter.Ne(
                publication => publication.Status,
                SharePublicationStatus.Draft);
    }

    internal static UpdateDefinition<SharePublicationDocument> BuildDraftCorrectionUpdate(
        DateTime updatedAtUtc)
    {
        return Builders<SharePublicationDocument>.Update
            .Pull(
                publication => publication.ContentPolicy.IncludedFields,
                ShareContentField.Avatar)
            .Inc(publication => publication.Version, 1)
            .Max(publication => publication.UpdatedAt, updatedAtUtc);
    }

    internal static UpdateDefinition<SharePublicationDocument> BuildVersionedCorrectionUpdate(
        DateTime updatedAtUtc)
    {
        return BuildDraftCorrectionUpdate(updatedAtUtc)
            .Inc(publication => publication.PublicationVersion, 1);
    }

    private static FilterDefinition<SharePublicationDocument> BuildAffectedPolicyFilter()
    {
        return Builders<SharePublicationDocument>.Filter.Eq(
                publication => publication.Type,
                SharePublicationType.PersonalRanking)
            & Builders<SharePublicationDocument>.Filter.AnyEq(
                publication => publication.ContentPolicy.IncludedFields,
                ShareContentField.Avatar);
    }
}
