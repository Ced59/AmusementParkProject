using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class MongoShareAccountDeletionStore : IShareAccountDeletionStore
{
    private readonly IMongoCollection<BsonDocument> publications;
    private readonly IMongoCollection<BsonDocument> invitations;
    private readonly IMongoCollection<BsonDocument> comparisons;
    private readonly IMongoCollection<BsonDocument> snapshots;
    private readonly IMongoCollection<BsonDocument> moderationReports;
    private readonly IMongoCollection<BsonDocument> sourceRevisions;
    private readonly IMongoCollection<BsonDocument> passportScopeRegistrations;
    private readonly IMongoCollection<BsonDocument> legacyRankingShares;

    public MongoShareAccountDeletionStore(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.publications = database.GetCollection<BsonDocument>(
            settings.SharePublicationsCollectionName);
        this.invitations = database.GetCollection<BsonDocument>(
            settings.ProfileComparisonInvitationsCollectionName);
        this.comparisons = database.GetCollection<BsonDocument>(
            settings.ProfileComparisonsCollectionName);
        this.snapshots = database.GetCollection<BsonDocument>(
            settings.SharePublicationSnapshotsCollectionName);
        this.moderationReports = database.GetCollection<BsonDocument>(
            settings.ShareModerationReportsCollectionName);
        this.sourceRevisions = database.GetCollection<BsonDocument>(
            settings.ShareSourceRevisionsCollectionName);
        this.passportScopeRegistrations = database.GetCollection<BsonDocument>(
            settings.PassportProfileShareScopeRegistrationsCollectionName);
        this.legacyRankingShares = database.GetCollection<BsonDocument>(
            settings.UserRankingSharesCollectionName);
    }

    public async Task<long> PurgeAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(
            ownerUserId,
            nameof(ownerUserId));
        IReadOnlyCollection<BsonDocument> publicationReferences =
            await this.LoadPublicationReferencesAsync(
                normalizedOwnerUserId,
                cancellationToken);
        string[] publicationIds = publicationReferences
            .Select(static document => document["_id"].AsString)
            .ToArray();
        string[] comparisonIds = await this.LoadComparisonIdsAsync(
            normalizedOwnerUserId,
            cancellationToken);
        string[] registeredScopeKeys = await this.LoadRegisteredScopeKeysAsync(
            normalizedOwnerUserId,
            cancellationToken);
        string[] sourceScopeKeys = BuildSourceScopeKeys(
            normalizedOwnerUserId,
            publicationReferences,
            registeredScopeKeys);

        long deletedCount = 0;
        deletedCount += await DeleteByStringValuesAsync(
            this.snapshots,
            "publicationId",
            publicationIds,
            cancellationToken);
        deletedCount += await this.DeleteModerationReportsAsync(
            publicationIds,
            comparisonIds,
            cancellationToken);
        deletedCount += await DeleteByParticipantAsync(
            this.invitations,
            normalizedOwnerUserId,
            cancellationToken);
        deletedCount += await DeleteByParticipantAsync(
            this.comparisons,
            normalizedOwnerUserId,
            cancellationToken);
        deletedCount += await DeleteByOwnerAsync(
            this.passportScopeRegistrations,
            normalizedOwnerUserId,
            cancellationToken);
        deletedCount += await this.DeleteSourceRevisionsAsync(
            normalizedOwnerUserId,
            sourceScopeKeys,
            cancellationToken);
        deletedCount += await DeleteByFieldAsync(
            this.legacyRankingShares,
            "userId",
            normalizedOwnerUserId,
            cancellationToken);
        deletedCount += await DeleteByOwnerAsync(
            this.publications,
            normalizedOwnerUserId,
            cancellationToken);
        return deletedCount;
    }

    private async Task<IReadOnlyCollection<BsonDocument>> LoadPublicationReferencesAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq(
            "ownerUserId",
            ownerUserId);
        ProjectionDefinition<BsonDocument> projection = Builders<BsonDocument>.Projection
            .Include("_id")
            .Include("sourceScopeKey");
        return await this.publications
            .Find(filter)
            .Project(projection)
            .ToListAsync(cancellationToken);
    }

    private async Task<string[]> LoadComparisonIdsAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        FilterDefinition<BsonDocument> filter = BuildParticipantFilter(ownerUserId);
        List<BsonDocument> documents = await this.comparisons
            .Find(filter)
            .Project(Builders<BsonDocument>.Projection.Include("_id"))
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document["_id"].AsString).ToArray();
    }

    private async Task<string[]> LoadRegisteredScopeKeysAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        List<BsonDocument> documents = await this.passportScopeRegistrations
            .Find(Builders<BsonDocument>.Filter.Eq("ownerUserId", ownerUserId))
            .Project(Builders<BsonDocument>.Projection.Include("scopeKey"))
            .ToListAsync(cancellationToken);
        return documents
            .Where(static document =>
                document.TryGetValue("scopeKey", out BsonValue? value)
                && value.IsString
                && !string.IsNullOrWhiteSpace(value.AsString))
            .Select(static document => document["scopeKey"].AsString)
            .ToArray();
    }

    private async Task<long> DeleteModerationReportsAsync(
        IReadOnlyCollection<string> publicationIds,
        IReadOnlyCollection<string> comparisonIds,
        CancellationToken cancellationToken)
    {
        FilterDefinition<BsonDocument>? filter = BuildModerationReportFilter(
            publicationIds,
            comparisonIds);
        if (filter is null)
        {
            return 0;
        }

        DeleteResult result = await this.moderationReports.DeleteManyAsync(
            filter,
            cancellationToken);
        return result.DeletedCount;
    }

    private async Task<long> DeleteSourceRevisionsAsync(
        string ownerUserId,
        IReadOnlyCollection<string> exactScopeKeys,
        CancellationToken cancellationToken)
    {
        DeleteResult result = await this.sourceRevisions.DeleteManyAsync(
            BuildSourceRevisionFilter(ownerUserId, exactScopeKeys),
            cancellationToken);
        return result.DeletedCount;
    }

    internal static FilterDefinition<BsonDocument>? BuildModerationReportFilter(
        IReadOnlyCollection<string> publicationIds,
        IReadOnlyCollection<string> comparisonIds)
    {
        ArgumentNullException.ThrowIfNull(publicationIds);
        ArgumentNullException.ThrowIfNull(comparisonIds);
        FilterDefinitionBuilder<BsonDocument> filters = Builders<BsonDocument>.Filter;
        List<FilterDefinition<BsonDocument>> targets = new List<FilterDefinition<BsonDocument>>();
        if (publicationIds.Count > 0)
        {
            string[] publicationTargetTypes = Enum
                .GetValues<ShareModerationTargetType>()
                .Where(static targetType =>
                    targetType != ShareModerationTargetType.ProfileComparison)
                .Select(static targetType => targetType.ToString())
                .ToArray();
            targets.Add(
                filters.In("targetRecordId", publicationIds)
                & filters.In("targetType", publicationTargetTypes));
        }

        if (comparisonIds.Count > 0)
        {
            targets.Add(
                filters.In("targetRecordId", comparisonIds)
                & filters.Eq(
                    "targetType",
                    ShareModerationTargetType.ProfileComparison.ToString()));
        }

        return targets.Count == 0 ? null : filters.Or(targets);
    }

    internal static string[] BuildSourceScopeKeys(
        string ownerUserId,
        IEnumerable<BsonDocument> publicationReferences,
        IEnumerable<string>? registeredScopeKeys = null)
    {
        IEnumerable<string> publicationScopes = publicationReferences
            .Where(static document =>
                document.TryGetValue("sourceScopeKey", out BsonValue? value)
                && value.IsString
                && !string.IsNullOrWhiteSpace(value.AsString))
            .Select(static document => document["sourceScopeKey"].AsString);
        return publicationScopes
            .Concat(registeredScopeKeys ?? Array.Empty<string>())
            .Where(static scopeKey => !string.IsNullOrWhiteSpace(scopeKey))
            .Select(static scopeKey => scopeKey.Trim())
            .Append(PersonalRankingShareSourceScope.Create(ownerUserId))
            .Append(PublicIdentityShareSourceScope.CreateDisplayName(ownerUserId))
            .Append(PublicIdentityShareSourceScope.CreateAvatar(ownerUserId))
            .Append(PassportProfileShareSourceScope.Create(ownerUserId))
            .Append(PassportProfileShareSourceScope.CreateCoordination(ownerUserId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    internal static FilterDefinition<BsonDocument> BuildSourceRevisionFilter(
        string ownerUserId,
        IReadOnlyCollection<string> exactScopeKeys)
    {
        ArgumentNullException.ThrowIfNull(exactScopeKeys);
        FilterDefinitionBuilder<BsonDocument> filters = Builders<BsonDocument>.Filter;
        string[] ownerPrefixes = new[]
        {
            VisitRecapShareSourceScope.CreateOwnerPrefix(ownerUserId),
            YearRecapShareSourceScope.CreateOwnerPrefix(ownerUserId),
            PassportProfileShareSourceScope.CreateFingerprintOwnerPrefix(ownerUserId),
            PersonalRankingShareSourceScope.CreateRatingOwnerPrefix(ownerUserId),
        };
        List<FilterDefinition<BsonDocument>> candidates = ownerPrefixes
            .Select(prefix =>
                filters.Gte("_id", prefix)
                & filters.Lt("_id", string.Concat(prefix, '\uffff')))
            .ToList();
        if (exactScopeKeys.Count > 0)
        {
            candidates.Add(filters.In("_id", exactScopeKeys));
        }

        return filters.Or(candidates);
    }

    private static async Task<long> DeleteByOwnerAsync(
        IMongoCollection<BsonDocument> collection,
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        return await DeleteByFieldAsync(
            collection,
            "ownerUserId",
            ownerUserId,
            cancellationToken);
    }

    private static async Task<long> DeleteByParticipantAsync(
        IMongoCollection<BsonDocument> collection,
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        DeleteResult result = await collection.DeleteManyAsync(
            BuildParticipantFilter(ownerUserId),
            cancellationToken);
        return result.DeletedCount;
    }

    private static async Task<long> DeleteByFieldAsync(
        IMongoCollection<BsonDocument> collection,
        string fieldName,
        string value,
        CancellationToken cancellationToken)
    {
        DeleteResult result = await collection.DeleteManyAsync(
            Builders<BsonDocument>.Filter.Eq(fieldName, value),
            cancellationToken);
        return result.DeletedCount;
    }

    private static async Task<long> DeleteByStringValuesAsync(
        IMongoCollection<BsonDocument> collection,
        string fieldName,
        IReadOnlyCollection<string> values,
        CancellationToken cancellationToken)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        DeleteResult result = await collection.DeleteManyAsync(
            Builders<BsonDocument>.Filter.In(fieldName, values),
            cancellationToken);
        return result.DeletedCount;
    }

    internal static FilterDefinition<BsonDocument> BuildParticipantFilter(string userId)
    {
        FilterDefinitionBuilder<BsonDocument> filters = Builders<BsonDocument>.Filter;
        return filters.Eq("creatorUserId", userId)
            | filters.Eq("acceptorUserId", userId);
    }
}
