using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class LivePollingStateRepository : ILivePollingStateRepository
{
    private readonly IMongoCollection<LivePollingStateDocument> collection;

    public LivePollingStateRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<LivePollingStateDocument>(
            settings.LivePollingStatesCollectionName);
    }

    public async Task<LivePollingStateSnapshot?> GetAsync(
        LiveDataSourceId sourceId,
        string externalEntityId,
        CancellationToken cancellationToken)
    {
        string normalizedExternalEntityId = externalEntityId?.Trim() ?? string.Empty;
        if (normalizedExternalEntityId.Length == 0)
        {
            return null;
        }

        DateTime nowUtc = DateTime.UtcNow;
        LivePollingStateDocument? document = await this.collection
            .Find(item => item.SourceId == sourceId.Value
                && item.ExternalEntityId == normalizedExternalEntityId)
            .FirstOrDefaultAsync(cancellationToken);
        return document is null
            ? null
            : new LivePollingStateSnapshot(
                sourceId,
                document.ExternalEntityId,
                document.NextAttemptAtUtc,
                document.LastPolledAtUtc,
                document.LastSuccessfulPollAtUtc,
                document.ConsecutiveFailures,
                document.CircuitOpenUntilUtc,
                document.LastDisposition,
                document.LeaseExpiresAtUtc.HasValue && document.LeaseExpiresAtUtc > nowUtc,
                document.LeaseExpiresAtUtc);
    }

    public async Task<LivePollingLease?> TryAcquireAsync(
        LivePollingLeaseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string leaseToken = Guid.NewGuid().ToString("N");
        FilterDefinition<LivePollingStateDocument> targetFilter =
            Builders<LivePollingStateDocument>.Filter.Eq(
                static document => document.SourceId,
                request.SourceId.Value)
            & Builders<LivePollingStateDocument>.Filter.Eq(
                static document => document.ExternalEntityId,
                request.ExternalEntityId);
        FilterDefinition<LivePollingStateDocument> dueFilter =
            Builders<LivePollingStateDocument>.Filter.Eq(
                static document => document.NextAttemptAtUtc,
                null)
            | Builders<LivePollingStateDocument>.Filter.Lte(
                static document => document.NextAttemptAtUtc,
                request.NowUtc);
        FilterDefinition<LivePollingStateDocument> circuitFilter =
            Builders<LivePollingStateDocument>.Filter.Eq(
                static document => document.CircuitOpenUntilUtc,
                null)
            | Builders<LivePollingStateDocument>.Filter.Lte(
                static document => document.CircuitOpenUntilUtc,
                request.NowUtc);
        FilterDefinition<LivePollingStateDocument> leaseFilter =
            Builders<LivePollingStateDocument>.Filter.Eq(
                static document => document.LeaseExpiresAtUtc,
                null)
            | Builders<LivePollingStateDocument>.Filter.Lte(
                static document => document.LeaseExpiresAtUtc,
                request.NowUtc);
        UpdateDefinition<LivePollingStateDocument> leaseUpdate =
            Builders<LivePollingStateDocument>.Update
                .Set(static document => document.LeaseOwner, request.LeaseOwner)
                .Set(static document => document.LeaseToken, leaseToken)
                .Set(
                    static document => document.LeaseExpiresAtUtc,
                    request.NowUtc.Add(request.LeaseDuration))
                .Set(
                    static document => document.NextAttemptAtUtc,
                    request.NowUtc.Add(request.CrashRecoveryCooldown))
                .Set(static document => document.UpdatedAt, request.NowUtc);
        LivePollingStateDocument? existing = await this.collection.FindOneAndUpdateAsync(
            targetFilter & dueFilter & circuitFilter & leaseFilter,
            leaseUpdate,
            new FindOneAndUpdateOptions<LivePollingStateDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (existing is not null)
        {
            return ToLease(existing);
        }

        LivePollingStateDocument? replaced = await this.TryReplaceTargetAsync(
            request,
            leaseToken,
            dueFilter,
            circuitFilter,
            leaseFilter,
            cancellationToken);
        if (replaced is not null)
        {
            return ToLease(replaced);
        }

        LivePollingStateDocument created = new LivePollingStateDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            SourceId = request.SourceId.Value,
            ExternalEntityId = request.ExternalEntityId,
            LeaseOwner = request.LeaseOwner,
            LeaseToken = leaseToken,
            LeaseExpiresAtUtc = request.NowUtc.Add(request.LeaseDuration),
            NextAttemptAtUtc = request.NowUtc.Add(request.CrashRecoveryCooldown),
            CreatedAt = request.NowUtc,
            UpdatedAt = request.NowUtc,
        };
        try
        {
            await this.collection.InsertOneAsync(created, cancellationToken: cancellationToken);
            return ToLease(created);
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return null;
        }
    }

    private async Task<LivePollingStateDocument?> TryReplaceTargetAsync(
        LivePollingLeaseRequest request,
        string leaseToken,
        FilterDefinition<LivePollingStateDocument> dueFilter,
        FilterDefinition<LivePollingStateDocument> circuitFilter,
        FilterDefinition<LivePollingStateDocument> leaseFilter,
        CancellationToken cancellationToken)
    {
        FilterDefinition<LivePollingStateDocument> replacementFilter =
            Builders<LivePollingStateDocument>.Filter.Eq(
                static document => document.SourceId,
                request.SourceId.Value)
            & Builders<LivePollingStateDocument>.Filter.Ne(
                static document => document.ExternalEntityId,
                request.ExternalEntityId)
            & dueFilter
            & circuitFilter
            & leaseFilter;
        UpdateDefinition<LivePollingStateDocument> replacementUpdate =
            Builders<LivePollingStateDocument>.Update
                .Set(static document => document.ExternalEntityId, request.ExternalEntityId)
                .Set(static document => document.LeaseOwner, request.LeaseOwner)
                .Set(static document => document.LeaseToken, leaseToken)
                .Set(
                    static document => document.LeaseExpiresAtUtc,
                    request.NowUtc.Add(request.LeaseDuration))
                .Set(
                    static document => document.NextAttemptAtUtc,
                    request.NowUtc.Add(request.CrashRecoveryCooldown))
                .Set(static document => document.ConsecutiveFailures, 0)
                .Set(static document => document.UpdatedAt, request.NowUtc)
                .Unset(static document => document.EntityTag)
                .Unset(static document => document.LastPolledAtUtc)
                .Unset(static document => document.LastSuccessfulPollAtUtc)
                .Unset(static document => document.CircuitOpenUntilUtc)
                .Unset(static document => document.LastDisposition);
        return await this.collection.FindOneAndUpdateAsync(
            replacementFilter,
            replacementUpdate,
            new FindOneAndUpdateOptions<LivePollingStateDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
    }

    public async Task<bool> CompleteAsync(
        LivePollingCompletion completion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(completion);
        FilterDefinition<LivePollingStateDocument> filter =
            Builders<LivePollingStateDocument>.Filter.Eq(
                static document => document.SourceId,
                completion.Lease.SourceId.Value)
            & Builders<LivePollingStateDocument>.Filter.Eq(
                static document => document.ExternalEntityId,
                completion.Lease.ExternalEntityId)
            & Builders<LivePollingStateDocument>.Filter.Eq(
                static document => document.LeaseOwner,
                completion.Lease.LeaseOwner)
            & Builders<LivePollingStateDocument>.Filter.Eq(
                static document => document.LeaseToken,
                completion.Lease.LeaseToken)
            & Builders<LivePollingStateDocument>.Filter.Gt(
                static document => document.LeaseExpiresAtUtc,
                completion.CompletedAtUtc);
        UpdateDefinition<LivePollingStateDocument> update =
            Builders<LivePollingStateDocument>.Update
                .Set(static document => document.NextAttemptAtUtc, completion.NextAttemptAtUtc)
                .Set(static document => document.ConsecutiveFailures, completion.ConsecutiveFailures)
                .Set(static document => document.CircuitOpenUntilUtc, completion.CircuitOpenUntilUtc)
                .Set(static document => document.LastDisposition, completion.Disposition)
                .Set(static document => document.UpdatedAt, completion.CompletedAtUtc)
                .Unset(static document => document.LeaseOwner)
                .Unset(static document => document.LeaseToken)
                .Unset(static document => document.LeaseExpiresAtUtc);
        if (completion.Disposition is not LivePollingCompletionDisposition.OutsideActiveWindow
            and not LivePollingCompletionDisposition.Suspended)
        {
            update = update.Set(
                static document => document.LastPolledAtUtc,
                completion.CompletedAtUtc);
        }

        if (completion.LastSuccessfulPollAtUtc is not null)
        {
            update = update.Set(
                static document => document.LastSuccessfulPollAtUtc,
                completion.LastSuccessfulPollAtUtc);
        }

        if (completion.ReplaceEntityTag)
        {
            update = completion.EntityTag is null
                ? update.Unset(static document => document.EntityTag)
                : update.Set(static document => document.EntityTag, completion.EntityTag);
        }

        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    private static LivePollingLease ToLease(LivePollingStateDocument document)
    {
        return new LivePollingLease(
            LiveDataSourceId.Parse(document.SourceId),
            document.ExternalEntityId,
            document.LeaseOwner ?? throw new InvalidOperationException("The polling lease owner is missing."),
            document.LeaseToken ?? throw new InvalidOperationException("The polling lease token is missing."),
            document.EntityTag,
            document.LastSuccessfulPollAtUtc,
            document.ConsecutiveFailures,
            document.CircuitOpenUntilUtc);
    }
}
