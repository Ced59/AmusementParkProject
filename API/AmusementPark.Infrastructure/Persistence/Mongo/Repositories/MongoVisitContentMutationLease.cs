using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class MongoVisitContentMutationLease : IVisitContentMutationLease
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
                    completedAtUtc.Add(MongoVisitContentMutationLeaseManager.LeaseDuration));
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
                renewedAtUtc.Add(MongoVisitContentMutationLeaseManager.LeaseDuration));
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = false },
            this.heartbeatCancellation.Token);
        return result.MatchedCount == 1;
    }
}
