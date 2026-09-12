using System.Collections.Concurrent;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ShareSourceRevisionRepository : IShareSourceRevisionRepository, IDisposable
{
    internal static readonly TimeSpan MutationLeaseDuration = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan MutationHeartbeatInterval = TimeSpan.FromMinutes(1);
    internal static readonly TimeSpan WriterLeaseCancellationDelay =
        MutationLeaseDuration - MutationHeartbeatInterval;

    private readonly IMongoCollection<ShareSourceRevisionDocument> collection;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ShareSourceRevisionRepository> logger;
    private readonly TimeSpan heartbeatInterval;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> heartbeatCancellations =
        new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.Ordinal);

    public ShareSourceRevisionRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        ILogger<ShareSourceRevisionRepository> logger)
        : this(
            database.GetCollection<ShareSourceRevisionDocument>(
                settings.ShareSourceRevisionsCollectionName),
            TimeProvider.System,
            logger,
            MutationHeartbeatInterval)
    {
    }

    internal ShareSourceRevisionRepository(
        IMongoCollection<ShareSourceRevisionDocument> collection,
        TimeProvider timeProvider)
        : this(
            collection,
            timeProvider,
            NullLogger<ShareSourceRevisionRepository>.Instance,
            MutationHeartbeatInterval)
    {
    }

    internal ShareSourceRevisionRepository(
        IMongoCollection<ShareSourceRevisionDocument> collection,
        TimeProvider timeProvider,
        ILogger<ShareSourceRevisionRepository> logger,
        TimeSpan heartbeatInterval)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (heartbeatInterval <= TimeSpan.Zero
            || heartbeatInterval >= MutationLeaseDuration)
        {
            throw new ArgumentOutOfRangeException(nameof(heartbeatInterval));
        }

        this.heartbeatInterval = heartbeatInterval;
    }

    public async Task<ShareSourceMutationLease?> TryBeginMutationAsync(
        string scopeKey,
        CancellationToken cancellationToken)
    {
        string normalizedScopeKey = NormalizeScopeKey(scopeKey);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        await this.RecoverExpiredLeasesAsync(normalizedScopeKey, nowUtc, cancellationToken);

        ShareSourceMutationLease mutationLease = ShareSourceMutationLease.Create(
            normalizedScopeKey);
        ShareSourceMutationLeaseDocument leaseDocument = new ShareSourceMutationLeaseDocument
        {
            Token = mutationLease.Token,
            ExpiresAtUtc = nowUtc.Add(MutationLeaseDuration),
        };
        UpdateDefinition<ShareSourceRevisionDocument> update =
            Builders<ShareSourceRevisionDocument>.Update
                .Set(document => document.UpdatedAt, nowUtc)
                .Push(document => document.MutationLeases, leaseDocument);
        ShareSourceRevisionDocument? document = await this.collection.FindOneAndUpdateAsync(
            ShareSourceRevisionMongoDefinitions.BuildScopeFilter(normalizedScopeKey),
            update,
            new FindOneAndUpdateOptions<ShareSourceRevisionDocument>
            {
                IsUpsert = false,
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (document is null)
        {
            return null;
        }

        CancellationTokenSource leaseCancellation = new CancellationTokenSource();
        leaseCancellation.CancelAfter(WriterLeaseCancellationDelay);
        mutationLease = new ShareSourceMutationLease(
            mutationLease.ScopeKey,
            mutationLease.Token,
            leaseCancellation.Token);
        this.StartHeartbeat(
            mutationLease,
            leaseCancellation,
            leaseDocument.ExpiresAtUtc);
        return mutationLease;
    }

    public async Task<ShareSourceMutationLease> BeginMutationAsync(
        string scopeKey,
        CancellationToken cancellationToken)
    {
        string normalizedScopeKey = NormalizeScopeKey(scopeKey);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        await this.RecoverExpiredLeasesAsync(normalizedScopeKey, nowUtc, cancellationToken);

        ShareSourceMutationLease mutationLease = ShareSourceMutationLease.Create(
            normalizedScopeKey);
        ShareSourceMutationLeaseDocument leaseDocument = new ShareSourceMutationLeaseDocument
        {
            Token = mutationLease.Token,
            ExpiresAtUtc = nowUtc.Add(MutationLeaseDuration),
        };
        UpdateDefinition<ShareSourceRevisionDocument> update =
            Builders<ShareSourceRevisionDocument>.Update
                .SetOnInsert(document => document.ScopeKey, normalizedScopeKey)
                .SetOnInsert(document => document.Revision, 0)
                .SetOnInsert(document => document.CreatedAt, nowUtc)
                .Set(document => document.UpdatedAt, nowUtc)
                .Push(document => document.MutationLeases, leaseDocument);
        FindOneAndUpdateOptions<ShareSourceRevisionDocument> options =
            new FindOneAndUpdateOptions<ShareSourceRevisionDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After,
            };
        ShareSourceRevisionDocument? document;
        try
        {
            document = await this.collection.FindOneAndUpdateAsync(
                ShareSourceRevisionMongoDefinitions.BuildScopeFilter(normalizedScopeKey),
                update,
                options,
                cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            options.IsUpsert = false;
            document = await this.collection.FindOneAndUpdateAsync(
                ShareSourceRevisionMongoDefinitions.BuildScopeFilter(normalizedScopeKey),
                update,
                options,
                cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            options.IsUpsert = false;
            document = await this.collection.FindOneAndUpdateAsync(
                ShareSourceRevisionMongoDefinitions.BuildScopeFilter(normalizedScopeKey),
                update,
                options,
                cancellationToken);
        }
        if (document is null)
        {
            throw new InvalidOperationException("Unable to reserve the share source mutation.");
        }

        CancellationTokenSource leaseCancellation = new CancellationTokenSource();
        leaseCancellation.CancelAfter(WriterLeaseCancellationDelay);
        mutationLease = new ShareSourceMutationLease(
            mutationLease.ScopeKey,
            mutationLease.Token,
            leaseCancellation.Token);
        this.StartHeartbeat(
            mutationLease,
            leaseCancellation,
            leaseDocument.ExpiresAtUtc);
        return mutationLease;
    }

    public async Task<ShareSourceRevision> CompleteMutationAsync(
        ShareSourceMutationLease mutationLease,
        bool sourceChanged,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutationLease);
        try
        {
            return await this.CompleteMutationCoreAsync(
                mutationLease,
                sourceChanged,
                cancellationToken);
        }
        finally
        {
            this.StopHeartbeat(mutationLease.Token);
        }
    }

    private async Task<ShareSourceRevision> CompleteMutationCoreAsync(
        ShareSourceMutationLease mutationLease,
        bool sourceChanged,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        FilterDefinition<ShareSourceRevisionDocument> filter =
            ShareSourceRevisionMongoDefinitions.BuildActiveLeaseFilter(
                mutationLease.ScopeKey,
                mutationLease.Token);
        if (sourceChanged)
        {
            filter &= Builders<ShareSourceRevisionDocument>.Filter.Lt(
                document => document.Revision,
                long.MaxValue);
        }

        UpdateDefinition<ShareSourceRevisionDocument> update =
            Builders<ShareSourceRevisionDocument>.Update
                .PullFilter(
                    document => document.MutationLeases,
                    lease => lease.Token == mutationLease.Token)
                .Set(document => document.UpdatedAt, nowUtc);
        if (sourceChanged)
        {
            update = update.Inc(document => document.Revision, 1);
        }

        ShareSourceRevisionDocument? document = await this.collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<ShareSourceRevisionDocument>
            {
                IsUpsert = false,
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (document is not null)
        {
            return ToResult(document);
        }

        ShareSourceRevisionDocument? recoveredDocument =
            await this.RecoverExpiredLeaseAsync(
                mutationLease,
                nowUtc,
                cancellationToken);
        if (recoveredDocument is not null)
        {
            return ToResult(recoveredDocument);
        }

        if (sourceChanged)
        {
            UpdateDefinition<ShareSourceRevisionDocument> fallbackUpdate =
                Builders<ShareSourceRevisionDocument>.Update
                    .Inc(value => value.Revision, 1)
                    .Set(value => value.UpdatedAt, nowUtc);
            ShareSourceRevisionDocument? advancedDocument = await this.collection.FindOneAndUpdateAsync(
                ShareSourceRevisionMongoDefinitions.BuildScopeFilter(mutationLease.ScopeKey)
                    & Builders<ShareSourceRevisionDocument>.Filter.Lt(
                        value => value.Revision,
                        long.MaxValue),
                fallbackUpdate,
                new FindOneAndUpdateOptions<ShareSourceRevisionDocument>
                {
                    IsUpsert = false,
                    ReturnDocument = ReturnDocument.After,
                },
                cancellationToken);
            if (advancedDocument is not null)
            {
                return ToResult(advancedDocument);
            }
        }

        ShareSourceRevision current = await this.GetOrCreateAsync(
            mutationLease.ScopeKey,
            cancellationToken);
        if (sourceChanged && current.Revision == long.MaxValue)
        {
            throw new InvalidOperationException("The share source revision cannot be incremented further.");
        }
        return current;
    }

    private async Task<ShareSourceRevisionDocument?> RecoverExpiredLeaseAsync(
        ShareSourceMutationLease mutationLease,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        UpdateDefinition<ShareSourceRevisionDocument> update =
            Builders<ShareSourceRevisionDocument>.Update
                .PullFilter(
                    document => document.MutationLeases,
                    lease => lease.Token == mutationLease.Token)
                .Inc(document => document.Revision, 1)
                .Set(document => document.UpdatedAt, nowUtc);
        return await this.collection.FindOneAndUpdateAsync(
            ShareSourceRevisionMongoDefinitions.BuildExpiredLeaseFilter(
                mutationLease.ScopeKey,
                mutationLease.Token),
            update,
            new FindOneAndUpdateOptions<ShareSourceRevisionDocument>
            {
                IsUpsert = false,
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
    }

    public async Task<ShareSourceRevision> GetOrCreateAsync(
        string scopeKey,
        CancellationToken cancellationToken)
    {
        string normalizedScopeKey = NormalizeScopeKey(scopeKey);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        await this.RecoverExpiredLeasesAsync(normalizedScopeKey, nowUtc, cancellationToken);

        UpdateDefinition<ShareSourceRevisionDocument> update =
            Builders<ShareSourceRevisionDocument>.Update
                .SetOnInsert(document => document.ScopeKey, normalizedScopeKey)
                .SetOnInsert(document => document.Revision, 0)
                .SetOnInsert(document => document.MutationLeases, new List<ShareSourceMutationLeaseDocument>())
                .SetOnInsert(document => document.CreatedAt, nowUtc)
                .SetOnInsert(document => document.UpdatedAt, nowUtc);
        FindOneAndUpdateOptions<ShareSourceRevisionDocument> options =
            new FindOneAndUpdateOptions<ShareSourceRevisionDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After,
            };
        ShareSourceRevisionDocument? document;
        try
        {
            document = await this.collection.FindOneAndUpdateAsync(
                ShareSourceRevisionMongoDefinitions.BuildScopeFilter(normalizedScopeKey),
                update,
                options,
                cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            options.IsUpsert = false;
            document = await this.collection.FindOneAndUpdateAsync(
                ShareSourceRevisionMongoDefinitions.BuildScopeFilter(normalizedScopeKey),
                update,
                options,
                cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            options.IsUpsert = false;
            document = await this.collection.FindOneAndUpdateAsync(
                ShareSourceRevisionMongoDefinitions.BuildScopeFilter(normalizedScopeKey),
                update,
                options,
                cancellationToken);
        }

        if (document is null)
        {
            throw new InvalidOperationException("Unable to read the share source revision.");
        }

        return ToResult(document);
    }

    public async Task EnsureCreatedAsync(
        IReadOnlyCollection<string> scopeKeys,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scopeKeys);
        string[] normalizedScopeKeys = scopeKeys
            .Select(NormalizeScopeKey)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedScopeKeys.Length == 0)
        {
            return;
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        WriteModel<ShareSourceRevisionDocument>[] writes = normalizedScopeKeys
            .Select(scopeKey =>
            {
                UpdateDefinition<ShareSourceRevisionDocument> update =
                    Builders<ShareSourceRevisionDocument>.Update
                        .SetOnInsert(document => document.ScopeKey, scopeKey)
                        .SetOnInsert(document => document.Revision, 0)
                        .SetOnInsert(
                            document => document.MutationLeases,
                            new List<ShareSourceMutationLeaseDocument>())
                        .SetOnInsert(document => document.CreatedAt, nowUtc)
                        .SetOnInsert(document => document.UpdatedAt, nowUtc);
                return (WriteModel<ShareSourceRevisionDocument>)
                    new UpdateOneModel<ShareSourceRevisionDocument>(
                        ShareSourceRevisionMongoDefinitions.BuildScopeFilter(scopeKey),
                        update)
                    {
                        IsUpsert = true,
                    };
            })
            .ToArray();

        try
        {
            await this.collection.BulkWriteAsync(
                writes,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken);
        }
        catch (MongoBulkWriteException<ShareSourceRevisionDocument> exception)
            when (exception.WriteErrors.Count > 0
                && exception.WriteConcernError is null
                && exception.WriteErrors.All(static error => error.Code == 11000))
        {
            // A concurrent initializer created the same scopes. The desired state is reached.
        }
    }

    public async Task<ShareSourceRevision> ReconcileFingerprintAsync(
        string scopeKey,
        string sourceFingerprint,
        CancellationToken cancellationToken)
    {
        string normalizedScopeKey = NormalizeScopeKey(scopeKey);
        string normalizedFingerprint = IdentifierRules.NormalizeRequired(
            sourceFingerprint,
            nameof(sourceFingerprint));
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        ShareSourceRevisionDocument? existing = await this.GetDocumentAsync(
            normalizedScopeKey,
            cancellationToken);
        if (existing is not null
            && string.Equals(
                existing.SourceFingerprint,
                normalizedFingerprint,
                StringComparison.Ordinal))
        {
            return ToSnapshotResult(existing, nowUtc);
        }

        if (existing is not null)
        {
            await this.RecoverExpiredLeasesAsync(
                normalizedScopeKey,
                nowUtc,
                cancellationToken);
        }

        UpdateDefinition<ShareSourceRevisionDocument> update =
            BuildFingerprintReconciliationUpdate(normalizedFingerprint, nowUtc);
        FindOneAndUpdateOptions<ShareSourceRevisionDocument> options =
            new FindOneAndUpdateOptions<ShareSourceRevisionDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After,
            };
        ShareSourceRevisionDocument? document;
        try
        {
            document = await this.collection.FindOneAndUpdateAsync(
                ShareSourceRevisionMongoDefinitions.BuildScopeFilter(normalizedScopeKey),
                update,
                options,
                cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            options.IsUpsert = false;
            document = await this.collection.FindOneAndUpdateAsync(
                ShareSourceRevisionMongoDefinitions.BuildScopeFilter(normalizedScopeKey),
                update,
                options,
                cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            options.IsUpsert = false;
            document = await this.collection.FindOneAndUpdateAsync(
                ShareSourceRevisionMongoDefinitions.BuildScopeFilter(normalizedScopeKey),
                update,
                options,
                cancellationToken);
        }

        if (document is null)
        {
            throw new InvalidOperationException("Unable to reconcile the share source fingerprint.");
        }

        return ToResult(document);
    }

    public async Task<IReadOnlyDictionary<string, ShareSourceRevision>> GetSnapshotAsync(
        IReadOnlyCollection<string> scopeKeys,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scopeKeys);
        string[] normalizedScopeKeys = scopeKeys
            .Select(NormalizeScopeKey)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Dictionary<string, ShareSourceRevision> snapshot =
            new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal);
        if (normalizedScopeKeys.Length == 0)
        {
            return snapshot;
        }

        FilterDefinition<ShareSourceRevisionDocument> filter =
            Builders<ShareSourceRevisionDocument>.Filter.In(
                document => document.ScopeKey,
                normalizedScopeKeys);
        using IAsyncCursor<ShareSourceRevisionDocument> cursor = await this.collection.FindAsync(
            filter,
            cancellationToken: cancellationToken);
        List<ShareSourceRevisionDocument> documents = await cursor.ToListAsync(cancellationToken);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        foreach (ShareSourceRevisionDocument document in documents)
        {
            snapshot[document.ScopeKey] = ToSnapshotResult(document, nowUtc);
        }

        foreach (string normalizedScopeKey in normalizedScopeKeys)
        {
            snapshot.TryAdd(
                normalizedScopeKey,
                new ShareSourceRevision(0, 0, DateTime.UnixEpoch));
        }

        return snapshot;
    }

    private static string NormalizeScopeKey(string scopeKey)
    {
        return IdentifierRules.NormalizeRequired(scopeKey, nameof(scopeKey));
    }

    private async Task<ShareSourceRevisionDocument?> GetDocumentAsync(
        string scopeKey,
        CancellationToken cancellationToken)
    {
        FindOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument> options =
            new FindOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument>
            {
                Limit = 1,
            };
        using IAsyncCursor<ShareSourceRevisionDocument> cursor =
            await this.collection.FindAsync(
                ShareSourceRevisionMongoDefinitions.BuildScopeFilter(scopeKey),
                options,
                cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    internal static UpdateDefinition<ShareSourceRevisionDocument> BuildHeartbeatUpdate(
        DateTime heartbeatAtUtc)
    {
        return Builders<ShareSourceRevisionDocument>.Update
            .Set("mutationLeases.$[lease].expiresAtUtc", heartbeatAtUtc.Add(MutationLeaseDuration))
            .Set(document => document.UpdatedAt, heartbeatAtUtc);
    }

    internal static UpdateDefinition<ShareSourceRevisionDocument>
        BuildFingerprintReconciliationUpdate(
            string sourceFingerprint,
            DateTime updatedAtUtc)
    {
        string normalizedFingerprint = IdentifierRules.NormalizeRequired(
            sourceFingerprint,
            nameof(sourceFingerprint));
        BsonDocument currentFingerprint = new BsonDocument(
            "$ifNull",
            new BsonArray { "$sourceFingerprint", BsonNull.Value });
        BsonDocument fingerprintMatches = new BsonDocument(
            "$eq",
            new BsonArray { currentFingerprint, normalizedFingerprint });
        BsonDocument currentRevision = new BsonDocument(
            "$ifNull",
            new BsonArray { "$revision", 0L });
        BsonDocument reconciledRevision = new BsonDocument(
            "$cond",
            new BsonArray
            {
                fingerprintMatches,
                currentRevision,
                new BsonDocument("$add", new BsonArray { currentRevision, 1L }),
            });
        BsonDocument reconciledUpdatedAt = new BsonDocument(
            "$cond",
            new BsonArray
            {
                fingerprintMatches,
                new BsonDocument(
                    "$ifNull",
                    new BsonArray { "$updatedAt", updatedAtUtc }),
                updatedAtUtc,
            });
        return new PipelineUpdateDefinition<ShareSourceRevisionDocument>(
            new[]
            {
                new BsonDocument(
                    "$set",
                    new BsonDocument
                    {
                        { "revision", reconciledRevision },
                        { "sourceFingerprint", normalizedFingerprint },
                        {
                            "mutationLeases",
                            new BsonDocument(
                                "$ifNull",
                                new BsonArray { "$mutationLeases", new BsonArray() })
                        },
                        {
                            "createdAt",
                            new BsonDocument(
                                "$ifNull",
                                new BsonArray { "$createdAt", updatedAtUtc })
                        },
                        { "updatedAt", reconciledUpdatedAt },
                    }),
            });
    }

    internal static BsonDocument BuildHeartbeatArrayFilter(string mutationToken)
    {
        return new BsonDocument("lease.token", NormalizeScopeKey(mutationToken));
    }

    private static UpdateOptions BuildHeartbeatUpdateOptions(string mutationToken)
    {
        return new UpdateOptions
        {
            ArrayFilters = new ArrayFilterDefinition[]
            {
                new BsonDocumentArrayFilterDefinition<ShareSourceMutationLeaseDocument>(
                    BuildHeartbeatArrayFilter(mutationToken)),
            },
        };
    }

    private void StartHeartbeat(
        ShareSourceMutationLease mutationLease,
        CancellationTokenSource leaseCancellation,
        DateTime confirmedExpiresAtUtc)
    {
        if (!this.heartbeatCancellations.TryAdd(
                mutationLease.Token,
                leaseCancellation))
        {
            leaseCancellation.Dispose();
            throw new InvalidOperationException("The share source mutation heartbeat is already active.");
        }

        _ = this.RunHeartbeatAsync(
            mutationLease,
            leaseCancellation,
            confirmedExpiresAtUtc);
    }

    private void StopHeartbeat(string mutationToken)
    {
        if (this.heartbeatCancellations.TryRemove(
                mutationToken,
                out CancellationTokenSource? cancellation))
        {
            cancellation.Cancel();
        }
    }

    private async Task RunHeartbeatAsync(
        ShareSourceMutationLease mutationLease,
        CancellationTokenSource cancellation,
        DateTime confirmedExpiresAtUtc)
    {
        try
        {
            while (true)
            {
                await Task.Delay(
                    this.heartbeatInterval,
                    this.timeProvider,
                    cancellation.Token);
                bool renewed = false;
                while (!renewed)
                {
                    if (this.timeProvider.GetUtcNow().UtcDateTime >= confirmedExpiresAtUtc)
                    {
                        cancellation.Cancel();
                        return;
                    }

                    try
                    {
                        DateTime heartbeatAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
                        UpdateResult result = await this.collection.UpdateOneAsync(
                            ShareSourceRevisionMongoDefinitions.BuildActiveLeaseFilter(
                                mutationLease.ScopeKey,
                                mutationLease.Token),
                            BuildHeartbeatUpdate(heartbeatAtUtc),
                            BuildHeartbeatUpdateOptions(mutationLease.Token),
                            cancellation.Token);
                        if (result.MatchedCount == 0)
                        {
                            cancellation.Cancel();
                            return;
                        }

                        confirmedExpiresAtUtc = heartbeatAtUtc.Add(MutationLeaseDuration);
                        cancellation.CancelAfter(WriterLeaseCancellationDelay);
                        renewed = true;
                    }
                    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception exception)
                    {
                        this.logger.LogWarning(
                            exception,
                            "Unable to renew the share source mutation lease for {ScopeKey}; renewal will be retried.",
                            mutationLease.ScopeKey);
                        TimeSpan retryInterval = GetHeartbeatRetryInterval(this.heartbeatInterval);
                        DateTime retryAtUtc = this.timeProvider.GetUtcNow().UtcDateTime.Add(retryInterval);
                        if (retryAtUtc >= confirmedExpiresAtUtc)
                        {
                            cancellation.Cancel();
                            return;
                        }

                        await Task.Delay(
                            retryInterval,
                            this.timeProvider,
                            cancellation.Token);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            this.heartbeatCancellations.TryRemove(mutationLease.Token, out _);
            cancellation.Dispose();
        }
    }

    internal static TimeSpan GetHeartbeatRetryInterval(TimeSpan heartbeatInterval)
    {
        TimeSpan maximumRetryInterval = TimeSpan.FromSeconds(5);
        return heartbeatInterval < maximumRetryInterval
            ? heartbeatInterval
            : maximumRetryInterval;
    }

    public void Dispose()
    {
        foreach (CancellationTokenSource cancellation in this.heartbeatCancellations.Values)
        {
            cancellation.Cancel();
        }

        this.heartbeatCancellations.Clear();
    }

    private async Task RecoverExpiredLeasesAsync(
        string scopeKey,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        UpdateDefinition<ShareSourceRevisionDocument> update =
            Builders<ShareSourceRevisionDocument>.Update
                .PullFilter(
                    document => document.MutationLeases,
                    lease => lease.ExpiresAtUtc <= nowUtc)
                .Inc(document => document.Revision, 1)
                .Set(document => document.UpdatedAt, nowUtc);
        await this.collection.UpdateOneAsync(
            ShareSourceRevisionMongoDefinitions.BuildExpiredLeaseFilter(scopeKey, nowUtc),
            update,
            cancellationToken: cancellationToken);
    }

    private static ShareSourceRevision ToResult(ShareSourceRevisionDocument document)
    {
        return new ShareSourceRevision(
            document.Revision,
            document.MutationLeases.Count,
            document.UpdatedAt);
    }

    private static ShareSourceRevision ToSnapshotResult(
        ShareSourceRevisionDocument document,
        DateTime nowUtc)
    {
        int activeMutationCount = document.MutationLeases.Count(
            lease => lease.ExpiresAtUtc > nowUtc);
        bool hasExpiredMutation = activeMutationCount < document.MutationLeases.Count;
        if (!hasExpiredMutation)
        {
            return new ShareSourceRevision(
                document.Revision,
                activeMutationCount,
                document.UpdatedAt);
        }

        if (document.Revision == long.MaxValue)
        {
            return new ShareSourceRevision(
                document.Revision,
                Math.Max(1, activeMutationCount),
                document.UpdatedAt);
        }

        return new ShareSourceRevision(
            document.Revision + 1,
            activeMutationCount,
            document.UpdatedAt);
    }
}
