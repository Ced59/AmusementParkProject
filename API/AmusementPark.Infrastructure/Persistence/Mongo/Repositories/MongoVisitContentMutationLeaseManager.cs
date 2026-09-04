using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class MongoVisitContentMutationLeaseManager :
    IVisitContentMutationLeaseManager
{
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan LeaseRenewalInterval = TimeSpan.FromMinutes(1);

    private readonly IMongoCollection<UserVisitDocument> collection;
    private readonly IMongoCollection<UserRideOccurrenceDocument> occurrenceCollection;
    private readonly IMongoCollection<UserRideOccurrenceCreationOperationDocument>
        operationCollection;
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan renewalInterval;

    public MongoVisitContentMutationLeaseManager(
        IMongoDatabase database,
        MongoDbSettings settings)
        : this(database, settings, TimeProvider.System, LeaseRenewalInterval)
    {
    }

    internal MongoVisitContentMutationLeaseManager(
        IMongoDatabase database,
        MongoDbSettings settings,
        TimeProvider timeProvider,
        TimeSpan renewalInterval)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<UserVisitDocument>(
            settings.UserVisitsCollectionName);
        this.occurrenceCollection = database.GetCollection<UserRideOccurrenceDocument>(
            settings.UserRideOccurrencesCollectionName);
        this.operationCollection =
            database.GetCollection<UserRideOccurrenceCreationOperationDocument>(
                settings.UserRideOccurrenceOperationsCollectionName);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (renewalInterval <= TimeSpan.Zero || renewalInterval >= LeaseDuration)
        {
            throw new ArgumentOutOfRangeException(nameof(renewalInterval));
        }

        this.timeProvider = timeProvider;
        this.renewalInterval = renewalInterval;
    }

    public async Task<IVisitContentMutationLease?> TryAcquireAsync(
        Visit visit,
        DateTime acquiredAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        if (acquiredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The lease timestamp must be UTC.", nameof(acquiredAtUtc));
        }

        if (visit.Status != VisitStatus.Draft)
        {
            return null;
        }

        string leaseToken = Guid.NewGuid().ToString("N");
        UserVisitDocument? acquired = await this.TryAcquireStableFenceAsync(
            visit,
            acquiredAtUtc,
            leaseToken,
            cancellationToken);
        bool requiresPromotion = acquired is null;
        if (requiresPromotion)
        {
            acquired = await this.TryAcquireRecoveryFenceAsync(
                visit,
                acquiredAtUtc,
                leaseToken,
                cancellationToken);
        }

        if (acquired?.ContentMutationFenceToken is null or < 1)
        {
            return null;
        }

        MongoVisitContentMutationLease lease = new MongoVisitContentMutationLease(
            this.collection,
            visit.Id.Value,
            visit.UserId,
            leaseToken,
            acquired.ContentMutationFenceToken.Value,
            this.timeProvider,
            this.renewalInterval);
        if (!requiresPromotion)
        {
            return lease;
        }

        try
        {
            await this.PromoteContentFenceAsync(
                visit,
                acquired.ContentMutationFenceToken.Value,
                acquired.ContentMutationFenceStableToken,
                cancellationToken);
            if (await lease.TryCompletePromotionAsync())
            {
                return lease;
            }

            await lease.DisposeAsync();
            return null;
        }
        catch
        {
            await lease.DisposeAsync();
            throw;
        }
    }

    private async Task<UserVisitDocument?> TryAcquireStableFenceAsync(
        Visit visit,
        DateTime acquiredAtUtc,
        string leaseToken,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<UserVisitDocument> filters =
            Builders<UserVisitDocument>.Filter;
        FilterDefinition<UserVisitDocument> stableFence =
            filters.Exists(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath, false)
            & filters.Eq(UserVisitMongoDefinitions.ContentMutationFenceReadyPath, true)
            & filters.Gte(UserVisitMongoDefinitions.ContentMutationFenceTokenPath, 1L)
            & filters.Gte(UserVisitMongoDefinitions.ContentMutationFenceStableTokenPath, 1L)
            & new BsonDocumentFilterDefinition<UserVisitDocument>(
                new BsonDocument(
                    "$expr",
                    new BsonDocument(
                        "$eq",
                        new BsonArray
                        {
                            $"${UserVisitMongoDefinitions.ContentMutationFenceTokenPath}",
                            $"${UserVisitMongoDefinitions.ContentMutationFenceStableTokenPath}",
                        })));
        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildOwnedVersionFilter(
                visit.Id.Value,
                visit.UserId,
                visit.Version)
            & filters.Eq(static document => document.Status, VisitStatus.Draft)
            & stableFence;
        UpdateDefinition<UserVisitDocument> update =
            Builders<UserVisitDocument>.Update
                .Set(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath, leaseToken)
                .Set(
                    UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath,
                    acquiredAtUtc.Add(LeaseDuration));
        return await this.collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<UserVisitDocument>
            {
                IsUpsert = false,
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
    }

    private async Task<UserVisitDocument?> TryAcquireRecoveryFenceAsync(
        Visit visit,
        DateTime acquiredAtUtc,
        string leaseToken,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<UserVisitDocument> filters =
            Builders<UserVisitDocument>.Filter;
        FilterDefinition<UserVisitDocument> availableLease =
            filters.Exists(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath, false)
            | filters.Exists(
                UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath,
                false)
            | filters.Lte(
                UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath,
                acquiredAtUtc);
        FilterDefinition<UserVisitDocument> fenceRequiresRecovery =
            filters.Exists(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath, true)
            | filters.Ne(UserVisitMongoDefinitions.ContentMutationFenceReadyPath, true)
            | filters.Exists(UserVisitMongoDefinitions.ContentMutationFenceTokenPath, false)
            | filters.Lt(UserVisitMongoDefinitions.ContentMutationFenceTokenPath, 1L)
            | filters.Exists(
                UserVisitMongoDefinitions.ContentMutationFenceStableTokenPath,
                false)
            | filters.Lt(
                UserVisitMongoDefinitions.ContentMutationFenceStableTokenPath,
                1L)
            | new BsonDocumentFilterDefinition<UserVisitDocument>(
                new BsonDocument(
                    "$expr",
                    new BsonDocument(
                        "$ne",
                        new BsonArray
                        {
                            $"${UserVisitMongoDefinitions.ContentMutationFenceTokenPath}",
                            $"${UserVisitMongoDefinitions.ContentMutationFenceStableTokenPath}",
                        })));
        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildOwnedVersionFilter(
                visit.Id.Value,
                visit.UserId,
                visit.Version)
            & filters.Eq(static document => document.Status, VisitStatus.Draft)
            & availableLease
            & fenceRequiresRecovery;
        UpdateDefinition<UserVisitDocument> update =
            Builders<UserVisitDocument>.Update
                .Set(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath, leaseToken)
                .Set(
                    UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath,
                    acquiredAtUtc.Add(LeaseDuration))
                .Inc(
                    UserVisitMongoDefinitions.ContentMutationFenceTokenPath,
                    1L)
                .Set(UserVisitMongoDefinitions.ContentMutationFenceReadyPath, false);
        return await this.collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<UserVisitDocument>
            {
                IsUpsert = false,
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
    }

    private async Task PromoteContentFenceAsync(
        Visit visit,
        long contentFenceToken,
        long? stableContentFenceToken,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> occurrenceFilters =
            Builders<UserRideOccurrenceDocument>.Filter;
        FilterDefinition<UserRideOccurrenceDocument> olderOccurrenceFence =
            stableContentFenceToken.HasValue
                ? occurrenceFilters.Gte(
                        static document => document.ContentMutationFenceToken,
                        stableContentFenceToken.Value)
                    & occurrenceFilters.Lt(
                        static document => document.ContentMutationFenceToken,
                        contentFenceToken)
                : occurrenceFilters.Or(
                    occurrenceFilters.Exists(
                        static document => document.ContentMutationFenceToken,
                        false),
                    occurrenceFilters.Eq(
                        static document => document.ContentMutationFenceToken,
                        null),
                    occurrenceFilters.Gte(
                            static document => document.ContentMutationFenceToken,
                            1L)
                        & occurrenceFilters.Lt(
                            static document => document.ContentMutationFenceToken,
                            contentFenceToken));
        FilterDefinition<UserRideOccurrenceDocument> occurrenceFilter =
            occurrenceFilters.Eq(static document => document.VisitId, visit.Id.Value)
            & occurrenceFilters.Eq(static document => document.UserId, visit.UserId)
            & olderOccurrenceFence;
        UpdateDefinition<UserRideOccurrenceDocument> occurrenceUpdate =
            Builders<UserRideOccurrenceDocument>.Update.Set(
                static document => document.ContentMutationFenceToken,
                contentFenceToken);
        _ = await this.occurrenceCollection.UpdateManyAsync(
            occurrenceFilter,
            occurrenceUpdate,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);

        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> operationFilters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> olderOperationFence =
            stableContentFenceToken.HasValue
                ? operationFilters.Gte(
                        static document => document.ContentMutationFenceToken,
                        stableContentFenceToken.Value)
                    & operationFilters.Lt(
                        static document => document.ContentMutationFenceToken,
                        contentFenceToken)
                : operationFilters.Or(
                    operationFilters.Exists(
                        static document => document.ContentMutationFenceToken,
                        false),
                    operationFilters.Eq(
                        static document => document.ContentMutationFenceToken,
                        null),
                    operationFilters.Gte(
                            static document => document.ContentMutationFenceToken,
                            1L)
                        & operationFilters.Lt(
                            static document => document.ContentMutationFenceToken,
                            contentFenceToken));
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> operationFilter =
            operationFilters.Eq(static document => document.VisitId, visit.Id.Value)
            & operationFilters.Eq(static document => document.UserId, visit.UserId)
            & olderOperationFence;
        UpdateDefinition<UserRideOccurrenceCreationOperationDocument> operationUpdate =
            Builders<UserRideOccurrenceCreationOperationDocument>.Update.Set(
                static document => document.ContentMutationFenceToken,
                contentFenceToken);
        _ = await this.operationCollection.UpdateManyAsync(
            operationFilter,
            operationUpdate,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);

    }

    private sealed class MongoVisitContentMutationLease : IVisitContentMutationLease
    {
        private readonly IMongoCollection<UserVisitDocument> collection;
        private readonly string visitId;
        private readonly string userId;
        private readonly string token;
        private readonly long contentFenceToken;
        private readonly TimeProvider timeProvider;
        private readonly TimeSpan renewalInterval;
        private readonly CancellationTokenSource heartbeatCancellation =
            new CancellationTokenSource();
        private readonly CancellationTokenSource leaseLostCancellation =
            new CancellationTokenSource();
        private readonly Task heartbeatTask;
        private int mutationCompleted;
        private int released;

        public MongoVisitContentMutationLease(
            IMongoCollection<UserVisitDocument> collection,
            string visitId,
            string userId,
            string token,
            long contentFenceToken,
            TimeProvider timeProvider,
            TimeSpan renewalInterval)
        {
            this.collection = collection;
            this.visitId = visitId;
            this.userId = userId;
            this.token = token;
            this.contentFenceToken = contentFenceToken;
            this.timeProvider = timeProvider;
            this.renewalInterval = renewalInterval;
            this.heartbeatTask = this.MaintainLeaseAsync();
        }

        public string Token => this.token;

        public long ContentFenceToken => this.contentFenceToken;

        public CancellationToken LeaseLostToken => this.leaseLostCancellation.Token;

        public void MarkMutationCompleted()
        {
            if (!this.leaseLostCancellation.IsCancellationRequested)
            {
                Volatile.Write(ref this.mutationCompleted, 1);
            }
        }

        public async Task<bool> TryCompletePromotionAsync()
        {
            DateTime completedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            FilterDefinitionBuilder<UserVisitDocument> filters =
                Builders<UserVisitDocument>.Filter;
            FilterDefinition<UserVisitDocument> filter = filters.Eq(
                    static document => document.Id,
                    this.visitId)
                & filters.Eq(static document => document.UserId, this.userId)
                & filters.Eq(
                    UserVisitMongoDefinitions.ContentMutationLeaseTokenPath,
                    this.token)
                & filters.Eq(
                    UserVisitMongoDefinitions.ContentMutationFenceTokenPath,
                    this.contentFenceToken)
                & filters.Gt(
                    UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath,
                    completedAtUtc);
            UpdateDefinition<UserVisitDocument> update =
                Builders<UserVisitDocument>.Update
                    .Set(UserVisitMongoDefinitions.ContentMutationFenceReadyPath, true)
                    .Set(
                        UserVisitMongoDefinitions.ContentMutationFenceStableTokenPath,
                        this.contentFenceToken)
                    .Set(
                        UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath,
                        completedAtUtc.Add(LeaseDuration));
            UpdateResult result = await this.collection.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = false },
                this.heartbeatCancellation.Token);
            return result.MatchedCount == 1;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref this.released, 1) != 0)
            {
                return;
            }

            await this.heartbeatCancellation.CancelAsync();
            await this.heartbeatTask;

            FilterDefinitionBuilder<UserVisitDocument> filters =
                Builders<UserVisitDocument>.Filter;
            FilterDefinition<UserVisitDocument> filter = filters.Eq(
                    static document => document.Id,
                    this.visitId)
                & filters.Eq(static document => document.UserId, this.userId)
                & filters.Eq(
                    UserVisitMongoDefinitions.ContentMutationLeaseTokenPath,
                    this.token);
            UpdateDefinitionBuilder<UserVisitDocument> updates =
                Builders<UserVisitDocument>.Update;
            UpdateDefinition<UserVisitDocument> update =
                Volatile.Read(ref this.mutationCompleted) == 1
                    ? updates
                        .Unset(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath)
                        .Unset(UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath)
                    : updates
                        .Set(UserVisitMongoDefinitions.ContentMutationFenceReadyPath, false)
                        .Unset(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath)
                        .Unset(UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath);
            _ = await this.collection.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = false },
                CancellationToken.None);
            this.heartbeatCancellation.Dispose();
            this.leaseLostCancellation.Dispose();
        }

        private async Task MaintainLeaseAsync()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(
                        this.renewalInterval,
                        this.timeProvider,
                        this.heartbeatCancellation.Token);
                    DateTime renewedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
                    if (!await this.TryRenewAsync(renewedAtUtc))
                    {
                        await this.leaseLostCancellation.CancelAsync();
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
                when (this.heartbeatCancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
                when (exception is MongoException or TimeoutException)
            {
                await this.leaseLostCancellation.CancelAsync();
            }
        }

        private async Task<bool> TryRenewAsync(DateTime renewedAtUtc)
        {
            FilterDefinitionBuilder<UserVisitDocument> filters =
                Builders<UserVisitDocument>.Filter;
            FilterDefinition<UserVisitDocument> filter = filters.Eq(
                    static document => document.Id,
                    this.visitId)
                & filters.Eq(static document => document.UserId, this.userId)
                & filters.Eq(
                    UserVisitMongoDefinitions.ContentMutationLeaseTokenPath,
                    this.token)
                & filters.Gt(
                    UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath,
                    renewedAtUtc);
            UpdateDefinition<UserVisitDocument> update =
                Builders<UserVisitDocument>.Update.Set(
                    UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath,
                    renewedAtUtc.Add(LeaseDuration));
            UpdateResult result = await this.collection.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = false },
                this.heartbeatCancellation.Token);
            return result.MatchedCount == 1;
        }
    }
}
