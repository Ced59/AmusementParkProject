using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// MongoDB-backed distributed lock for current-image mutations.
/// </summary>
public sealed class MongoImageCurrentMutationLock : IImageCurrentMutationLock
{
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    internal static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);
    internal static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    private readonly IMongoCollection<ImageCurrentMutationLockDocument> collection;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<MongoImageCurrentMutationLock> logger;
    private readonly TimeSpan leaseDuration;
    private readonly TimeSpan heartbeatInterval;

    public MongoImageCurrentMutationLock(
        IMongoDatabase database,
        MongoDbSettings settings,
        ILogger<MongoImageCurrentMutationLock> logger)
        : this(
            database.GetCollection<ImageCurrentMutationLockDocument>(
                settings.ImageCurrentMutationLocksCollectionName),
            TimeProvider.System,
            logger)
    {
    }

    internal MongoImageCurrentMutationLock(
        IMongoCollection<ImageCurrentMutationLockDocument> collection,
        TimeProvider timeProvider,
        ILogger<MongoImageCurrentMutationLock> logger)
        : this(
            collection,
            timeProvider,
            logger,
            LeaseDuration,
            HeartbeatInterval)
    {
    }

    internal MongoImageCurrentMutationLock(
        IMongoCollection<ImageCurrentMutationLockDocument> collection,
        TimeProvider timeProvider,
        ILogger<MongoImageCurrentMutationLock> logger,
        TimeSpan heartbeatInterval)
        : this(
            collection,
            timeProvider,
            logger,
            LeaseDuration,
            heartbeatInterval)
    {
    }

    internal MongoImageCurrentMutationLock(
        IMongoCollection<ImageCurrentMutationLockDocument> collection,
        TimeProvider timeProvider,
        ILogger<MongoImageCurrentMutationLock> logger,
        TimeSpan leaseDuration,
        TimeSpan heartbeatInterval)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        if (heartbeatInterval <= TimeSpan.Zero || heartbeatInterval >= leaseDuration)
        {
            throw new ArgumentOutOfRangeException(nameof(heartbeatInterval));
        }

        this.leaseDuration = leaseDuration;
        this.heartbeatInterval = heartbeatInterval;
    }

    public Task<TResult> ExecuteAsync<TResult>(
        ImageOwnerType ownerType,
        string ownerId,
        ImageCategory category,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        return this.ExecuteAsync(
            ownerType,
            ownerId,
            category,
            operation,
            cancellationToken,
            CancellationToken.None);
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        ImageOwnerType ownerType,
        string ownerId,
        ImageCategory category,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken,
        CancellationToken consistencyCancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(operation);

        string scopeKey = BuildScopeKey(ownerType, ownerId, category);
        string leaseToken = Guid.NewGuid().ToString("N");
        using CancellationTokenSource acquisitionCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                consistencyCancellationToken);
        DateTime confirmedExpiresAtUtc = await this.AcquireAsync(
            scopeKey,
            leaseToken,
            acquisitionCancellation.Token);

        using CancellationTokenSource heartbeatCancellation = new CancellationTokenSource();
        using CancellationTokenSource leaseLostCancellation = new CancellationTokenSource();
        using CancellationTokenSource heartbeatLoopCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                heartbeatCancellation.Token,
                leaseLostCancellation.Token);
        this.ScheduleLeaseLossCancellation(
            leaseLostCancellation,
            confirmedExpiresAtUtc);
        Task heartbeat = this.MaintainLeaseAsync(
            scopeKey,
            leaseToken,
            confirmedExpiresAtUtc,
            heartbeatLoopCancellation.Token,
            leaseLostCancellation);
        using CancellationTokenSource operationCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                leaseLostCancellation.Token,
                consistencyCancellationToken);
        try
        {
            // Once the lock is held, finish or reconcile the short critical section even
            // if the HTTP caller disconnects. A proven image-lock loss or a separate
            // consistency guard may still interrupt it.
            TResult result = await operation(operationCancellation.Token);
            if (leaseLostCancellation.IsCancellationRequested)
            {
                throw new InvalidOperationException(
                    "The current-image mutation lock was lost before completion.");
            }

            consistencyCancellationToken.ThrowIfCancellationRequested();

            return result;
        }
        finally
        {
            heartbeatCancellation.Cancel();
            await AwaitHeartbeatAsync(heartbeat);
            await this.ReleaseSafelyAsync(scopeKey, leaseToken);
        }
    }

    internal static string BuildScopeKey(
        ImageOwnerType ownerType,
        string ownerId,
        ImageCategory category)
    {
        string normalizedOwnerId = ownerId.Trim();
        return $"{ownerType}:{normalizedOwnerId.Length}:{normalizedOwnerId}:{category}";
    }

    internal static FilterDefinition<ImageCurrentMutationLockDocument> BuildAcquireFilter(
        string scopeKey,
        DateTime nowUtc)
    {
        FilterDefinitionBuilder<ImageCurrentMutationLockDocument> builder =
            Builders<ImageCurrentMutationLockDocument>.Filter;
        return builder.Eq(static document => document.ScopeKey, scopeKey)
            & builder.Or(
                builder.Eq(static document => document.Token, null),
                builder.Lte(static document => document.ExpiresAtUtc, nowUtc));
    }

    internal static FilterDefinition<ImageCurrentMutationLockDocument> BuildOwnedLeaseFilter(
        string scopeKey,
        string leaseToken)
    {
        FilterDefinitionBuilder<ImageCurrentMutationLockDocument> builder =
            Builders<ImageCurrentMutationLockDocument>.Filter;
        return builder.Eq(static document => document.ScopeKey, scopeKey)
            & builder.Eq(static document => document.Token, leaseToken);
    }

    private async Task<DateTime> AcquireAsync(
        string scopeKey,
        string leaseToken,
        CancellationToken cancellationToken)
    {
        DateTime deadlineUtc = this.timeProvider.GetUtcNow().UtcDateTime.Add(WaitTimeout);
        while (true)
        {
            DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            DateTime? expiresAtUtc = await this.TryAcquireAsync(
                scopeKey,
                leaseToken,
                nowUtc,
                cancellationToken);
            if (expiresAtUtc.HasValue)
            {
                return expiresAtUtc.Value;
            }

            if (nowUtc >= deadlineUtc)
            {
                throw new TimeoutException(
                    "Timed out while waiting for the current-image mutation lock.");
            }

            await Task.Delay(RetryDelay, this.timeProvider, cancellationToken);
        }
    }

    private async Task<DateTime?> TryAcquireAsync(
        string scopeKey,
        string leaseToken,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        DateTime expiresAtUtc = nowUtc.Add(this.leaseDuration);
        UpdateDefinition<ImageCurrentMutationLockDocument> update =
            Builders<ImageCurrentMutationLockDocument>.Update
                .SetOnInsert(static document => document.ScopeKey, scopeKey)
                .Set(static document => document.Token, leaseToken)
                .Set(static document => document.ExpiresAtUtc, expiresAtUtc)
                .Set(static document => document.UpdatedAtUtc, nowUtc);
        try
        {
            ImageCurrentMutationLockDocument? document =
                await this.collection.FindOneAndUpdateAsync(
                    BuildAcquireFilter(scopeKey, nowUtc),
                    update,
                    new FindOneAndUpdateOptions<ImageCurrentMutationLockDocument>
                    {
                        IsUpsert = true,
                        ReturnDocument = ReturnDocument.After,
                    },
                    cancellationToken);
            return document is not null
                && string.Equals(document.Token, leaseToken, StringComparison.Ordinal)
                    ? expiresAtUtc
                    : null;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return null;
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            return null;
        }
    }

    private async Task MaintainLeaseAsync(
        string scopeKey,
        string leaseToken,
        DateTime confirmedExpiresAtUtc,
        CancellationToken cancellationToken,
        CancellationTokenSource leaseLostCancellation)
    {
        try
        {
            while (true)
            {
                await Task.Delay(this.heartbeatInterval, this.timeProvider, cancellationToken);
                bool renewed = false;
                while (!renewed)
                {
                    DateTime writerDeadlineUtc = confirmedExpiresAtUtc.Subtract(
                        this.heartbeatInterval);
                    if (this.timeProvider.GetUtcNow().UtcDateTime >= writerDeadlineUtc)
                    {
                        leaseLostCancellation.Cancel();
                        return;
                    }

                    try
                    {
                        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
                        UpdateResult result = await this.collection.UpdateOneAsync(
                            BuildOwnedLeaseFilter(scopeKey, leaseToken),
                            Builders<ImageCurrentMutationLockDocument>.Update
                                .Set(static document => document.ExpiresAtUtc, nowUtc.Add(this.leaseDuration))
                                .Set(static document => document.UpdatedAtUtc, nowUtc),
                            cancellationToken: cancellationToken);
                        if (result.MatchedCount == 0)
                        {
                            leaseLostCancellation.Cancel();
                            return;
                        }

                        confirmedExpiresAtUtc = nowUtc.Add(this.leaseDuration);
                        this.ScheduleLeaseLossCancellation(
                            leaseLostCancellation,
                            confirmedExpiresAtUtc);
                        renewed = true;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception exception)
                    {
                        this.logger.LogWarning(
                            exception,
                            "Unable to renew current-image mutation lock {ScopeKey}; renewal will be retried.",
                            scopeKey);
                        TimeSpan retryInterval = GetHeartbeatRetryInterval(
                            this.heartbeatInterval);
                        DateTime retryAtUtc = this.timeProvider.GetUtcNow().UtcDateTime.Add(
                            retryInterval);
                        if (retryAtUtc >= writerDeadlineUtc)
                        {
                            leaseLostCancellation.Cancel();
                            return;
                        }

                        await Task.Delay(
                            retryInterval,
                            this.timeProvider,
                            cancellationToken);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void ScheduleLeaseLossCancellation(
        CancellationTokenSource leaseLostCancellation,
        DateTime confirmedExpiresAtUtc)
    {
        DateTime writerDeadlineUtc = confirmedExpiresAtUtc.Subtract(
            this.heartbeatInterval);
        TimeSpan remaining = writerDeadlineUtc - this.timeProvider.GetUtcNow().UtcDateTime;
        if (remaining <= TimeSpan.Zero)
        {
            leaseLostCancellation.Cancel();
            return;
        }

        leaseLostCancellation.CancelAfter(remaining);
    }

    internal static TimeSpan GetHeartbeatRetryInterval(TimeSpan heartbeatInterval)
    {
        TimeSpan maximumRetryInterval = TimeSpan.FromSeconds(5);
        return heartbeatInterval < maximumRetryInterval
            ? heartbeatInterval
            : maximumRetryInterval;
    }

    private async Task ReleaseSafelyAsync(string scopeKey, string leaseToken)
    {
        try
        {
            await this.collection.UpdateOneAsync(
                BuildOwnedLeaseFilter(scopeKey, leaseToken),
                Builders<ImageCurrentMutationLockDocument>.Update
                    .Set(static document => document.Token, null)
                    .Set(static document => document.ExpiresAtUtc, null)
                    .Set(
                        static document => document.UpdatedAtUtc,
                        this.timeProvider.GetUtcNow().UtcDateTime),
                cancellationToken: CancellationToken.None);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Unable to release current-image mutation lock {ScopeKey}; it will expire.",
                scopeKey);
        }
    }

    private static async Task AwaitHeartbeatAsync(Task heartbeat)
    {
        try
        {
            await heartbeat;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
