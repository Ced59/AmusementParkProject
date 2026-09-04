using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class MongoVisitDeletionStore : IVisitDeletionStore
{
    internal const string DeletedAtUtcPath = "deletedAtUtc";
    internal const string PurgeScheduledForUtcPath = "purgeScheduledForUtc";
    internal const string DeletionOperationKeyHashPath = "deletionOperationKeyHash";
    private readonly IMongoCollection<UserVisitDocument> visits;
    private readonly IMongoCollection<BsonDocument> rawVisits;
    private readonly IMongoCollection<UserRideOccurrenceDocument> occurrences;
    private readonly IMongoCollection<UserRideOccurrenceCreationOperationDocument> operations;
    private readonly IMongoCollection<PassportAuditJournalDocument> auditEvents;

    public MongoVisitDeletionStore(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.visits = database.GetCollection<UserVisitDocument>(settings.UserVisitsCollectionName);
        this.rawVisits = database.GetCollection<BsonDocument>(settings.UserVisitsCollectionName);
        this.occurrences = database.GetCollection<UserRideOccurrenceDocument>(
            settings.UserRideOccurrencesCollectionName);
        this.operations = database.GetCollection<UserRideOccurrenceCreationOperationDocument>(
            settings.UserRideOccurrenceOperationsCollectionName);
        this.auditEvents = database.GetCollection<PassportAuditJournalDocument>(
            settings.PassportAuditEventsCollectionName);
    }

    public async Task<VisitDeletionImpact> GetImpactAsync(
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        FilterDefinition<UserRideOccurrenceDocument> activeOccurrences =
            filters.Eq(static document => document.VisitId, visitId.Value)
            & filters.Eq(static document => document.UserId, normalizedUserId)
            & filters.Eq(static document => document.DeletedAtUtc, null)
            & filters.Ne(static document => document.CreationPendingCompletion, true);
        long occurrenceCount = await this.occurrences.CountDocumentsAsync(
            activeOccurrences,
            cancellationToken: cancellationToken);
        long assessmentCount = await this.occurrences.CountDocumentsAsync(
            activeOccurrences & filters.Ne(static document => document.Assessment, null),
            cancellationToken: cancellationToken);
        return new VisitDeletionImpact(occurrenceCount, assessmentCount);
    }

    public async Task<VisitDeletionReceipt?> GetReceiptAsync(
        VisitId visitId,
        string userId,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string operationKeyHash = UserVisitCreationFingerprint.HashOperationKey(
            IdentifierRules.NormalizeRequired(clientOperationId, nameof(clientOperationId)));
        FilterDefinitionBuilder<BsonDocument> filters = Builders<BsonDocument>.Filter;
        BsonDocument? document = await this.rawVisits
            .Find(filters.Eq("_id", visitId.Value)
                & filters.Eq("userId", normalizedUserId)
                & filters.Eq(DeletionOperationKeyHashPath, operationKeyHash)
                & filters.Exists(DeletedAtUtcPath, true))
            .Project(BuildReceiptProjection())
            .FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return null;
        }

        return new VisitDeletionReceipt(
            visitId.Value,
            document[DeletedAtUtcPath].ToUniversalTime(),
            document[PurgeScheduledForUtcPath].ToUniversalTime(),
            true);
    }

    public async Task<bool> TryTombstoneAsync(
        VisitDeletionTombstoneRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateUtc(request.DeletedAtUtc, nameof(request));
        ValidateUtc(request.PurgeScheduledForUtc, nameof(request));
        if (!string.Equals(request.AuditEvent.UserId, request.UserId, StringComparison.Ordinal)
            || !string.Equals(
                request.AuditEvent.VisitId,
                request.VisitId.Value,
                StringComparison.Ordinal)
            || request.AuditEvent.EventType != PassportAuditEventType.VisitDeleted
            || request.AuditEvent.EntityVersion != request.ExpectedVersion + 1)
        {
            throw new ArgumentException("The deletion audit event does not match the visit.", nameof(request));
        }

        string operationKeyHash = UserVisitCreationFingerprint.HashOperationKey(
            IdentifierRules.NormalizeRequired(
                request.ClientOperationId,
                nameof(request.ClientOperationId)));
        UpdateDefinition<UserVisitDocument> update = BuildTombstoneUpdate(
            request,
            operationKeyHash);
        FilterDefinition<UserVisitDocument> visitFilter =
            string.IsNullOrWhiteSpace(request.ContentMutationLeaseToken)
                ? UserVisitMongoDefinitions.BuildOwnedMutableVersionFilter(
                    request.VisitId.Value,
                    request.UserId,
                    request.ExpectedVersion,
                    request.DeletedAtUtc)
                : UserVisitMongoDefinitions.BuildOwnedLeasedVersionFilter(
                    request.VisitId.Value,
                    request.UserId,
                    request.ExpectedVersion,
                    request.ContentMutationLeaseToken);
        UpdateResult result = await this.visits.UpdateOneAsync(
            visitFilter,
            update,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<VisitDeletionPurgeResult> PurgeBatchAsync(
        VisitId visitId,
        string userId,
        DateTime nowUtc,
        int maximumDocumentsPerCollection,
        CancellationToken cancellationToken)
    {
        ValidateUtc(nowUtc, nameof(nowUtc));
        if (maximumDocumentsPerCollection is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDocumentsPerCollection));
        }

        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinition<UserVisitDocument> ownedVisitFilter =
            UserVisitMongoDefinitions.BuildOwnedAnyStateVisitFilter(
                visitId.Value,
                normalizedUserId);
        BsonDocument? visitState = await this.visits.Find(ownedVisitFilter)
            .Project<BsonDocument>(Builders<UserVisitDocument>.Projection
                .Include(DeletedAtUtcPath)
                .Include(PurgeScheduledForUtcPath))
            .FirstOrDefaultAsync(cancellationToken);
        bool hasTombstone = visitState is not null
            && visitState.Contains(DeletedAtUtcPath)
            && visitState.Contains(PurgeScheduledForUtcPath);
        if (visitState is not null && !hasTombstone)
        {
            return new VisitDeletionPurgeResult(true, 0);
        }

        if (hasTombstone
            && visitState![PurgeScheduledForUtcPath].ToUniversalTime() > nowUtc)
        {
            return new VisitDeletionPurgeResult(false, 0);
        }

        int deletedCount = 0;
        deletedCount += await DeleteBatchAsync(
            this.operations,
            BuildOperationPurgeFilter(visitId.Value, normalizedUserId),
            maximumDocumentsPerCollection,
            cancellationToken);
        deletedCount += await DeleteBatchAsync(
            this.occurrences,
            BuildOccurrencePurgeFilter(visitId.Value, normalizedUserId),
            maximumDocumentsPerCollection,
            cancellationToken);
        deletedCount += await DeleteBatchAsync(
            this.auditEvents,
            BuildAuditPurgeFilter(visitId.Value, normalizedUserId),
            maximumDocumentsPerCollection,
            cancellationToken);

        bool hasRemainingChildren = await HasRemainingChildrenAsync(
            visitId.Value,
            normalizedUserId,
            cancellationToken);
        if (hasRemainingChildren)
        {
            return new VisitDeletionPurgeResult(false, deletedCount);
        }

        if (!hasTombstone)
        {
            return new VisitDeletionPurgeResult(true, deletedCount);
        }

        DeleteResult deletion = await this.visits.DeleteOneAsync(
            BuildTombstoneFilter(visitId, normalizedUserId)
                & Builders<UserVisitDocument>.Filter.Lte(
                    PurgeScheduledForUtcPath,
                    nowUtc),
            cancellationToken);
        return new VisitDeletionPurgeResult(
            deletion.DeletedCount == 1,
            deletedCount + checked((int)deletion.DeletedCount));
    }

    private async Task<bool> HasRemainingChildrenAsync(
        string visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        Task<bool> operationsRemain = this.operations.Find(
                BuildOperationPurgeFilter(visitId, userId))
            .Project(static document => document.Id)
            .AnyAsync(cancellationToken);
        Task<bool> occurrencesRemain = this.occurrences.Find(
                BuildOccurrencePurgeFilter(visitId, userId))
            .Project(static document => document.Id)
            .AnyAsync(cancellationToken);
        Task<bool> auditsRemain = this.auditEvents.Find(
                BuildAuditPurgeFilter(visitId, userId))
            .Project(static document => document.Id)
            .AnyAsync(cancellationToken);
        await Task.WhenAll(operationsRemain, occurrencesRemain, auditsRemain);
        return await operationsRemain || await occurrencesRemain || await auditsRemain;
    }

    private static async Task<int> DeleteBatchAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        int maximumDocuments,
        CancellationToken cancellationToken)
    {
        List<BsonDocument> idDocuments = await collection.Find(filter)
            .Project<BsonDocument>(Builders<TDocument>.Projection.Include("_id"))
            .Limit(maximumDocuments)
            .ToListAsync(cancellationToken);
        if (idDocuments.Count == 0)
        {
            return 0;
        }

        string[] ids = idDocuments
            .Select(static document => document["_id"].AsString)
            .ToArray();
        DeleteResult deletion = await collection.DeleteManyAsync(
            Builders<TDocument>.Filter.In("_id", ids),
            cancellationToken);
        return checked((int)deletion.DeletedCount);
    }

    internal static FilterDefinition<UserVisitDocument> BuildTombstoneFilter(
        VisitId visitId,
        string userId)
    {
        FilterDefinitionBuilder<UserVisitDocument> filters = Builders<UserVisitDocument>.Filter;
        return UserVisitMongoDefinitions.BuildOwnedAnyStateVisitFilter(visitId.Value, userId)
            & filters.Exists(DeletedAtUtcPath, true)
            & filters.Exists(PurgeScheduledForUtcPath, true);
    }

    internal static FilterDefinition<UserRideOccurrenceCreationOperationDocument>
        BuildOperationPurgeFilter(string visitId, string userId)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        return filters.Eq(static document => document.VisitId, visitId)
            & filters.Eq(static document => document.UserId, userId);
    }

    internal static FilterDefinition<UserRideOccurrenceDocument>
        BuildOccurrencePurgeFilter(string visitId, string userId)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        return filters.Eq(static document => document.VisitId, visitId)
            & filters.Eq(static document => document.UserId, userId);
    }

    internal static FilterDefinition<PassportAuditJournalDocument>
        BuildAuditPurgeFilter(string visitId, string userId)
    {
        FilterDefinitionBuilder<PassportAuditJournalDocument> filters =
            Builders<PassportAuditJournalDocument>.Filter;
        return filters.Eq(static document => document.Event.VisitId, visitId)
            & filters.Eq(static document => document.Event.UserId, userId);
    }

    internal static UpdateDefinition<UserVisitDocument> BuildTombstoneUpdate(
        VisitDeletionTombstoneRequest request,
        string operationKeyHash)
    {
        UpdateDefinitionBuilder<UserVisitDocument> updates =
            Builders<UserVisitDocument>.Update;
        return updates.Combine(
            updates.Set(DeletedAtUtcPath, request.DeletedAtUtc),
            updates.Set(PurgeScheduledForUtcPath, request.PurgeScheduledForUtc),
            updates.Set(DeletionOperationKeyHashPath, operationKeyHash),
            updates.Set(static document => document.Version, request.ExpectedVersion + 1),
            updates.Set(static document => document.UpdatedAt, request.DeletedAtUtc),
            updates.Push(
                static document => document.PendingAuditEvents,
                request.AuditEvent.ToDocument()),
            updates.Unset(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath),
            updates.Unset(UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath));
    }

    private static ProjectionDefinition<BsonDocument> BuildReceiptProjection()
    {
        return Builders<BsonDocument>.Projection
            .Include(DeletedAtUtcPath)
            .Include(PurgeScheduledForUtcPath);
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must be UTC.", parameterName);
        }
    }
}
