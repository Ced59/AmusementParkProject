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
    internal const string DeletedAtUtcPath = UserVisitMongoDefinitions.DeletedAtUtcPath;
    internal const string PurgeScheduledForUtcPath =
        UserVisitMongoDefinitions.PurgeScheduledForUtcPath;
    internal const string DeletionOperationKeyHashPath = "deletionOperationKeyHash";
    internal const string PurgeJobEnsuredAtUtcPath =
        UserVisitMongoDefinitions.PurgeJobEnsuredAtUtcPath;
    internal const string ExportInvalidationEnsuredAtUtcPath =
        UserVisitMongoDefinitions.ExportInvalidationEnsuredAtUtcPath;
    internal const string ExportInvalidationFenceAtUtcPath =
        UserVisitMongoDefinitions.ExportInvalidationFenceAtUtcPath;
    internal const string ExportInvalidationClaimTokenPath =
        UserVisitMongoDefinitions.ExportInvalidationClaimTokenPath;
    internal const string ExportInvalidationClaimExpiresAtUtcPath =
        UserVisitMongoDefinitions.ExportInvalidationClaimExpiresAtUtcPath;
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
            document["version"].AsInt64,
            true,
            HasTimestamp(document, ExportInvalidationEnsuredAtUtcPath));
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

    public async Task<IReadOnlyCollection<VisitDeletionReconciliationCandidate>>
        ListPendingDeletionReconciliationAsync(
            DateTime nowUtc,
            int maximumCount,
            CancellationToken cancellationToken)
    {
        ValidateUtc(nowUtc, nameof(nowUtc));
        if (maximumCount is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        List<BsonDocument> documents = await this.rawVisits
            .Find(BuildPendingDeletionReconciliationFilter(nowUtc))
            .Sort(Builders<BsonDocument>.Sort
                .Ascending(PurgeJobEnsuredAtUtcPath)
                .Ascending(PurgeScheduledForUtcPath)
                .Ascending("_id"))
            .Project(BuildPendingDeletionReconciliationProjection())
            .Limit(maximumCount)
            .ToListAsync(cancellationToken);
        return documents.Select(static document =>
            new VisitDeletionReconciliationCandidate(
                VisitId.Parse(document["_id"].AsString),
                document["userId"].AsString,
                document["version"].AsInt64,
                document[DeletedAtUtcPath].ToUniversalTime(),
                document[PurgeScheduledForUtcPath].ToUniversalTime(),
                HasTimestamp(document, ExportInvalidationEnsuredAtUtcPath),
                HasTimestamp(document, PurgeJobEnsuredAtUtcPath)))
            .ToArray();
    }

    public async Task<VisitExportInvalidationClaim?> TryClaimExportInvalidationAsync(
        VisitId visitId,
        string userId,
        long deletionVersion,
        DateTime claimedAtUtc,
        DateTime claimExpiresAtUtc,
        CancellationToken cancellationToken)
    {
        ValidateUtc(claimedAtUtc, nameof(claimedAtUtc));
        ValidateUtc(claimExpiresAtUtc, nameof(claimExpiresAtUtc));
        if (claimExpiresAtUtc <= claimedAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(claimExpiresAtUtc));
        }

        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string claimToken = Guid.NewGuid().ToString("N");
        FilterDefinitionBuilder<BsonDocument> filters = Builders<BsonDocument>.Filter;
        FilterDefinition<BsonDocument> baseFilter = BuildDeletionVersionFilter(
                filters,
                visitId,
                normalizedUserId,
                deletionVersion)
            & BuildMissingTimestampFilter(
                filters,
                ExportInvalidationEnsuredAtUtcPath);
        UpdateResult initialClaim = await this.rawVisits.UpdateOneAsync(
            baseFilter
                & BuildMissingTimestampFilter(
                    filters,
                    ExportInvalidationFenceAtUtcPath),
            Builders<BsonDocument>.Update
                .Set(ExportInvalidationFenceAtUtcPath, claimedAtUtc)
                .Set(ExportInvalidationClaimTokenPath, claimToken)
                .Set(ExportInvalidationClaimExpiresAtUtcPath, claimExpiresAtUtc),
            cancellationToken: cancellationToken);
        if (initialClaim.MatchedCount == 1)
        {
            return new VisitExportInvalidationClaim(claimToken, claimedAtUtc);
        }

        BsonDocument? recoveredClaim = await this.rawVisits.FindOneAndUpdateAsync(
            baseFilter
                & filters.Type(
                    ExportInvalidationFenceAtUtcPath,
                    BsonType.DateTime)
                & filters.Or(
                    filters.Exists(ExportInvalidationClaimTokenPath, false),
                    filters.Eq(ExportInvalidationClaimTokenPath, BsonNull.Value),
                    filters.Exists(ExportInvalidationClaimExpiresAtUtcPath, false),
                    filters.Lte(
                        ExportInvalidationClaimExpiresAtUtcPath,
                        claimedAtUtc)),
            Builders<BsonDocument>.Update
                .Set(ExportInvalidationClaimTokenPath, claimToken)
                .Set(ExportInvalidationClaimExpiresAtUtcPath, claimExpiresAtUtc),
            new FindOneAndUpdateOptions<BsonDocument, BsonDocument>
            {
                IsUpsert = false,
                ReturnDocument = ReturnDocument.After,
                Projection = Builders<BsonDocument>.Projection
                    .Include(ExportInvalidationFenceAtUtcPath),
            },
            cancellationToken);
        return recoveredClaim is null
            ? null
            : new VisitExportInvalidationClaim(
                claimToken,
                recoveredClaim[ExportInvalidationFenceAtUtcPath].ToUniversalTime());
    }

    public async Task<bool> CompleteExportInvalidationAsync(
        VisitId visitId,
        string userId,
        long deletionVersion,
        string claimToken,
        DateTime ensuredAtUtc,
        CancellationToken cancellationToken)
    {
        ValidateUtc(ensuredAtUtc, nameof(ensuredAtUtc));
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string normalizedClaimToken = IdentifierRules.NormalizeRequired(
            claimToken,
            nameof(claimToken));
        FilterDefinitionBuilder<BsonDocument> filters = Builders<BsonDocument>.Filter;
        UpdateResult result = await this.rawVisits.UpdateOneAsync(
            BuildDeletionVersionFilter(
                filters,
                visitId,
                normalizedUserId,
                deletionVersion)
                & filters.Eq(
                    ExportInvalidationClaimTokenPath,
                    normalizedClaimToken),
            Builders<BsonDocument>.Update
                .Set(ExportInvalidationEnsuredAtUtcPath, ensuredAtUtc)
                .Unset(ExportInvalidationClaimTokenPath)
                .Unset(ExportInvalidationClaimExpiresAtUtcPath),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    public Task<bool> MarkPurgeJobEnsuredAsync(
        VisitId visitId,
        string userId,
        long deletionVersion,
        DateTime ensuredAtUtc,
        CancellationToken cancellationToken)
    {
        return this.MarkDeletionSideEffectEnsuredAsync(
            visitId,
            userId,
            deletionVersion,
            PurgeJobEnsuredAtUtcPath,
            ensuredAtUtc,
            cancellationToken);
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

        string? auditMaintenanceLeaseToken = null;
        if (hasTombstone)
        {
            auditMaintenanceLeaseToken = Guid.NewGuid().ToString("N");
            bool leaseAcquired = await this.TryAcquireAuditMaintenanceLeaseAsync(
                visitId,
                normalizedUserId,
                auditMaintenanceLeaseToken,
                nowUtc,
                cancellationToken);
            if (!leaseAcquired)
            {
                return new VisitDeletionPurgeResult(false, 0);
            }
        }

        try
        {
            if (hasTombstone
                && await this.HasPendingAuditMarkersAsync(
                    visitId.Value,
                    normalizedUserId,
                    cancellationToken))
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
                        nowUtc)
                    & Builders<UserVisitDocument>.Filter.Eq(
                        UserVisitMongoDefinitions.AuditMaintenanceLeaseTokenPath,
                        auditMaintenanceLeaseToken),
                cancellationToken);
            return new VisitDeletionPurgeResult(
                deletion.DeletedCount == 1,
                deletedCount + checked((int)deletion.DeletedCount));
        }
        finally
        {
            if (auditMaintenanceLeaseToken is not null)
            {
                await this.ReleaseAuditMaintenanceLeaseAsync(
                    visitId,
                    normalizedUserId,
                    auditMaintenanceLeaseToken,
                    CancellationToken.None);
            }
        }
    }

    private async Task<bool> TryAcquireAuditMaintenanceLeaseAsync(
        VisitId visitId,
        string userId,
        string leaseToken,
        DateTime acquiredAtUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinition<UserVisitDocument> filter =
            BuildTombstoneFilter(visitId, userId)
            & UserVisitMongoDefinitions.BuildAvailableAuditMaintenanceLeaseFilter(
                acquiredAtUtc);
        UpdateResult result = await this.visits.UpdateOneAsync(
            filter,
            UserVisitMongoDefinitions.BuildAuditMaintenanceLeaseUpdate(
                leaseToken,
                acquiredAtUtc.Add(
                    PassportAuditStore.AuditMaintenanceLeaseDuration)),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    private async Task ReleaseAuditMaintenanceLeaseAsync(
        VisitId visitId,
        string userId,
        string leaseToken,
        CancellationToken cancellationToken)
    {
        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildOwnedAnyStateVisitFilter(
                visitId.Value,
                userId)
            & Builders<UserVisitDocument>.Filter.Eq(
                UserVisitMongoDefinitions.AuditMaintenanceLeaseTokenPath,
                leaseToken);
        await this.visits.UpdateOneAsync(
            filter,
            UserVisitMongoDefinitions.BuildAuditMaintenanceLeaseRelease(),
            cancellationToken: cancellationToken);
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

    private async Task<bool> HasPendingAuditMarkersAsync(
        string visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        Task<bool> visitMarkerExists = this.visits.Find(
                BuildVisitPendingAuditFilter(visitId, userId))
            .Project(static document => document.Id)
            .AnyAsync(cancellationToken);
        Task<bool> occurrenceMarkerExists = this.occurrences.Find(
                BuildOccurrencePendingAuditFilter(visitId, userId))
            .Project(static document => document.Id)
            .AnyAsync(cancellationToken);
        Task<bool> operationMarkerExists = this.operations.Find(
                BuildOperationPendingAuditFilter(visitId, userId))
            .Project(static document => document.Id)
            .AnyAsync(cancellationToken);
        await Task.WhenAll(
            visitMarkerExists,
            occurrenceMarkerExists,
            operationMarkerExists);
        return await visitMarkerExists
            || await occurrenceMarkerExists
            || await operationMarkerExists;
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

    internal static FilterDefinition<UserVisitDocument> BuildVisitPendingAuditFilter(
        string visitId,
        string userId)
    {
        return UserVisitMongoDefinitions.BuildOwnedAnyStateVisitFilter(visitId, userId)
            & Builders<UserVisitDocument>.Filter.Exists(
                PassportAuditMongoDefinitions.PendingEventIdPath,
                true);
    }

    internal static FilterDefinition<UserRideOccurrenceDocument>
        BuildOccurrencePendingAuditFilter(string visitId, string userId)
    {
        return BuildOccurrencePurgeFilter(visitId, userId)
            & Builders<UserRideOccurrenceDocument>.Filter.Exists(
                PassportAuditMongoDefinitions.PendingEventIdPath,
                true);
    }

    internal static FilterDefinition<UserRideOccurrenceCreationOperationDocument>
        BuildOperationPendingAuditFilter(string visitId, string userId)
    {
        return BuildOperationPurgeFilter(visitId, userId)
            & Builders<UserRideOccurrenceCreationOperationDocument>.Filter.Exists(
                PassportAuditMongoDefinitions.PendingEventIdPath,
                true);
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
            .Include(PurgeScheduledForUtcPath)
            .Include(ExportInvalidationEnsuredAtUtcPath)
            .Include("version");
    }

    internal static FilterDefinition<BsonDocument>
        BuildPendingDeletionReconciliationFilter(DateTime nowUtc)
    {
        ValidateUtc(nowUtc, nameof(nowUtc));
        FilterDefinitionBuilder<BsonDocument> filters = Builders<BsonDocument>.Filter;
        return filters.Exists(DeletedAtUtcPath, true)
            & filters.Exists(PurgeScheduledForUtcPath, true)
            & filters.Gt("version", 0)
            & filters.Or(
                BuildMissingTimestampFilter(filters, ExportInvalidationEnsuredAtUtcPath),
                BuildMissingTimestampFilter(filters, PurgeJobEnsuredAtUtcPath),
                filters.Lte(PurgeScheduledForUtcPath, nowUtc));
    }

    private static ProjectionDefinition<BsonDocument>
        BuildPendingDeletionReconciliationProjection()
    {
        return Builders<BsonDocument>.Projection
            .Include("_id")
            .Include("userId")
            .Include("version")
            .Include(DeletedAtUtcPath)
            .Include(PurgeScheduledForUtcPath)
            .Include(ExportInvalidationEnsuredAtUtcPath)
            .Include(PurgeJobEnsuredAtUtcPath);
    }

    private async Task<bool> MarkDeletionSideEffectEnsuredAsync(
        VisitId visitId,
        string userId,
        long deletionVersion,
        string timestampPath,
        DateTime ensuredAtUtc,
        CancellationToken cancellationToken)
    {
        ValidateUtc(ensuredAtUtc, nameof(ensuredAtUtc));
        if (deletionVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(deletionVersion));
        }

        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinitionBuilder<BsonDocument> filters = Builders<BsonDocument>.Filter;
        UpdateResult result = await this.rawVisits.UpdateOneAsync(
            BuildDeletionVersionFilter(
                filters,
                visitId,
                normalizedUserId,
                deletionVersion),
            Builders<BsonDocument>.Update.Set(timestampPath, ensuredAtUtc),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    private static FilterDefinition<BsonDocument> BuildDeletionVersionFilter(
        FilterDefinitionBuilder<BsonDocument> filters,
        VisitId visitId,
        string userId,
        long deletionVersion)
    {
        if (deletionVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(deletionVersion));
        }

        return filters.Eq("_id", visitId.Value)
            & filters.Eq("userId", userId)
            & filters.Eq("version", deletionVersion)
            & filters.Exists(DeletedAtUtcPath, true)
            & filters.Exists(PurgeScheduledForUtcPath, true);
    }

    private static FilterDefinition<BsonDocument> BuildMissingTimestampFilter(
        FilterDefinitionBuilder<BsonDocument> filters,
        string timestampPath)
    {
        return filters.Or(
            filters.Exists(timestampPath, false),
            filters.Eq(timestampPath, BsonNull.Value));
    }

    private static bool HasTimestamp(BsonDocument document, string path)
    {
        return document.TryGetValue(path, out BsonValue? value)
            && value.IsBsonDateTime;
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must be UTC.", parameterName);
        }
    }
}
