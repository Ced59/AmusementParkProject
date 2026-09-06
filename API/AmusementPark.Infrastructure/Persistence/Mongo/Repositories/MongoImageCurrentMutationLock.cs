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
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        ImageOwnerType ownerType,
        string ownerId,
        ImageCategory category,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(operation);

        string scopeKey = BuildScopeKey(ownerType, ownerId, category);
        string leaseToken = Guid.NewGuid().ToString("N");
        await this.AcquireAsync(scopeKey, leaseToken, cancellationToken);

        using CancellationTokenSource heartbeatCancellation = new CancellationTokenSource();
        using CancellationTokenSource leaseLostCancellation = new CancellationTokenSource();
        using CancellationTokenSource operationCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                leaseLostCancellation.Token);
        Task heartbeat = this.MaintainLeaseAsync(
            scopeKey,
            leaseToken,
            heartbeatCancellation.Token,
            leaseLostCancellation);
        try
        {
            TResult result = await operation(operationCancellation.Token);
            if (leaseLostCancellation.IsCancellationRequested)
            {
                throw new InvalidOperationException(
                    "The current-image mutation lock was lost before completion.");
            }

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

    private async Task AcquireAsync(
        string scopeKey,
        string leaseToken,
        CancellationToken cancellationToken)
    {
        DateTime deadlineUtc = this.timeProvider.GetUtcNow().UtcDateTime.Add(WaitTimeout);
        while (true)
        {
            DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            if (await this.TryAcquireAsync(scopeKey, leaseToken, nowUtc, cancellationToken))
            {
                return;
            }

            if (nowUtc >= deadlineUtc)
            {
                throw new TimeoutException(
                    "Timed out while waiting for the current-image mutation lock.");
            }

            await Task.Delay(RetryDelay, this.timeProvider, cancellationToken);
        }
    }

    private async Task<bool> TryAcquireAsync(
        string scopeKey,
        string leaseToken,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        UpdateDefinition<ImageCurrentMutationLockDocument> update =
            Builders<ImageCurrentMutationLockDocument>.Update
                .SetOnInsert(static document => document.ScopeKey, scopeKey)
                .Set(static document => document.Token, leaseToken)
                .Set(static document => document.ExpiresAtUtc, nowUtc.Add(LeaseDuration))
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
                && string.Equals(document.Token, leaseToken, StringComparison.Ordinal);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            return false;
        }
    }

    private async Task MaintainLeaseAsync(
        string scopeKey,
        string leaseToken,
        CancellationToken cancellationToken,
        CancellationTokenSource leaseLostCancellation)
    {
        try
        {
            while (true)
            {
                await Task.Delay(HeartbeatInterval, this.timeProvider, cancellationToken);
                DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
                UpdateResult result = await this.collection.UpdateOneAsync(
                    BuildOwnedLeaseFilter(scopeKey, leaseToken),
                    Builders<ImageCurrentMutationLockDocument>.Update
                        .Set(static document => document.ExpiresAtUtc, nowUtc.Add(LeaseDuration))
                        .Set(static document => document.UpdatedAtUtc, nowUtc),
                    cancellationToken: cancellationToken);
                if (result.MatchedCount == 0)
                {
                    leaseLostCancellation.Cancel();
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Unable to renew current-image mutation lock {ScopeKey}.",
                scopeKey);
            leaseLostCancellation.Cancel();
        }
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
