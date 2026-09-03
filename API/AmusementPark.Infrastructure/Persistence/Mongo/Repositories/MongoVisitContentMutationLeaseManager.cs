using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class MongoVisitContentMutationLeaseManager :
    IVisitContentMutationLeaseManager
{
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan LeaseRenewalInterval = TimeSpan.FromMinutes(1);

    private readonly IMongoCollection<UserVisitDocument> collection;
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

        string token = Guid.NewGuid().ToString("N");
        FilterDefinitionBuilder<UserVisitDocument> filters =
            Builders<UserVisitDocument>.Filter;
        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildOwnedVersionFilter(
                visit.Id.Value,
                visit.UserId,
                visit.Version)
            & filters.Eq(static document => document.Status, VisitStatus.Draft)
            & filters.Or(
                filters.Exists(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath, false),
                filters.Lte(
                    UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath,
                    acquiredAtUtc));
        UpdateDefinition<UserVisitDocument> update =
            Builders<UserVisitDocument>.Update
                .Set(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath, token)
                .Set(
                    UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath,
                    acquiredAtUtc.Add(LeaseDuration));
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1
            ? new MongoVisitContentMutationLease(
                this.collection,
                visit.Id.Value,
                visit.UserId,
                token,
                this.timeProvider,
                this.renewalInterval)
            : null;
    }

    private sealed class MongoVisitContentMutationLease : IVisitContentMutationLease
    {
        private readonly IMongoCollection<UserVisitDocument> collection;
        private readonly string visitId;
        private readonly string userId;
        private readonly string token;
        private readonly TimeProvider timeProvider;
        private readonly TimeSpan renewalInterval;
        private readonly CancellationTokenSource heartbeatCancellation =
            new CancellationTokenSource();
        private readonly CancellationTokenSource leaseLostCancellation =
            new CancellationTokenSource();
        private readonly Task heartbeatTask;
        private int released;

        public MongoVisitContentMutationLease(
            IMongoCollection<UserVisitDocument> collection,
            string visitId,
            string userId,
            string token,
            TimeProvider timeProvider,
            TimeSpan renewalInterval)
        {
            this.collection = collection;
            this.visitId = visitId;
            this.userId = userId;
            this.token = token;
            this.timeProvider = timeProvider;
            this.renewalInterval = renewalInterval;
            this.heartbeatTask = this.MaintainLeaseAsync();
        }

        public string Token => this.token;

        public CancellationToken LeaseLostToken => this.leaseLostCancellation.Token;

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
            UpdateDefinition<UserVisitDocument> update =
                Builders<UserVisitDocument>.Update
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
