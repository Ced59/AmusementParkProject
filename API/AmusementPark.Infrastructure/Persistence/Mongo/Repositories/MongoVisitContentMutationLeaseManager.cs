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


}
