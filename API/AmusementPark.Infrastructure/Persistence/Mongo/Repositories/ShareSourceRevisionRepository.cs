using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ShareSourceRevisionRepository : IShareSourceRevisionRepository
{
    internal static readonly TimeSpan MutationLeaseDuration = TimeSpan.FromMinutes(5);

    private readonly IMongoCollection<ShareSourceRevisionDocument> collection;
    private readonly TimeProvider timeProvider;

    public ShareSourceRevisionRepository(
        IMongoDatabase database,
        MongoDbSettings settings)
        : this(
            database.GetCollection<ShareSourceRevisionDocument>(
                settings.ShareSourceRevisionsCollectionName),
            TimeProvider.System)
    {
    }

    internal ShareSourceRevisionRepository(
        IMongoCollection<ShareSourceRevisionDocument> collection,
        TimeProvider timeProvider)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ShareSourceMutationLease> BeginMutationAsync(
        string scopeKey,
        CancellationToken cancellationToken)
    {
        string normalizedScopeKey = NormalizeScopeKey(scopeKey);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        await this.RecoverExpiredLeasesAsync(normalizedScopeKey, nowUtc, cancellationToken);

        ShareSourceMutationLease mutationLease = ShareSourceMutationLease.Create(normalizedScopeKey);
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

        return mutationLease;
    }

    public async Task<ShareSourceRevision> CompleteMutationAsync(
        ShareSourceMutationLease mutationLease,
        bool sourceChanged,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutationLease);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        FilterDefinition<ShareSourceRevisionDocument> filter =
            ShareSourceRevisionMongoDefinitions.BuildLeaseFilter(
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

        ShareSourceRevision current = await this.GetOrCreateAsync(
            mutationLease.ScopeKey,
            cancellationToken);
        if (sourceChanged && current.Revision == long.MaxValue && !current.IsStable)
        {
            throw new InvalidOperationException("The share source revision cannot be incremented further.");
        }

        return current;
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

    private static string NormalizeScopeKey(string scopeKey)
    {
        return IdentifierRules.NormalizeRequired(scopeKey, nameof(scopeKey));
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
}
