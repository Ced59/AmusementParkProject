using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class RatingRankingSourceRevisionRepository : IRatingRankingSourceRevisionRepository
{
    internal static readonly TimeSpan MutationLeaseDuration = TimeSpan.FromMinutes(5);

    private readonly IMongoCollection<RatingRankingSourceRevisionDocument> collection;
    private readonly TimeProvider timeProvider;

    public RatingRankingSourceRevisionRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);

        this.collection = database.GetCollection<RatingRankingSourceRevisionDocument>(
            settings.RatingRankingSourceRevisionsCollectionName);
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<RatingRankingMutationLease> BeginMutationAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        DateTime leaseExpiresAtUtc = nowUtc.Add(MutationLeaseDuration);
        RatingRankingMutationLease mutationLease = RatingRankingMutationLease.Create(scopeKey);
        FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument> options =
            new FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After,
            };
        RatingRankingSourceRevisionDocument? document = await this.collection.FindOneAndUpdateAsync(
            RatingRankingSourceRevisionMongoDefinitions.BuildScopeFilter(scopeKey),
            RatingRankingSourceRevisionMongoDefinitions.BuildBeginMutationUpdate(
                scopeKey,
                mutationLease,
                nowUtc,
                leaseExpiresAtUtc),
            options,
            cancellationToken);
        if (document is null)
        {
            throw new InvalidOperationException(
                $"The source mutation lease for ranking scope '{scopeKey.Value}' could not be acquired.");
        }

        return mutationLease;
    }

    public async Task<RatingRankingSourceRevision> CompleteMutationAsync(
        RatingRankingMutationLease mutationLease,
        bool sourceChanged,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutationLease);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument> options =
            new FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument>
            {
                ReturnDocument = ReturnDocument.After,
            };
        RatingRankingSourceRevisionDocument? document = await this.collection.FindOneAndUpdateAsync(
            RatingRankingSourceRevisionMongoDefinitions.BuildMutationLeaseFilter(mutationLease),
            RatingRankingSourceRevisionMongoDefinitions.BuildCompleteMutationUpdate(
                mutationLease,
                sourceChanged,
                nowUtc),
            options,
            cancellationToken);
        document ??= await this.collection
            .Find(RatingRankingSourceRevisionMongoDefinitions.BuildScopeFilter(
                mutationLease.ScopeKey))
            .FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            throw new InvalidOperationException(
                $"The source mutation lease for ranking scope '{mutationLease.ScopeKey.Value}' could not be completed.");
        }

        return ToApplication(document);
    }

    public async Task MarkUnavailableAsync(
        RankingScopeKey scopeKey,
        RatingMethodologyVersion methodologyVersion,
        long sourceRevision,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        if (sourceRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceRevision));
        }

        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("An unavailable reason code is required.", nameof(reasonCode));
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        FilterDefinition<RatingRankingSourceRevisionDocument> filter =
            RatingRankingSourceRevisionMongoDefinitions.BuildScopeFilter(scopeKey)
            & Builders<RatingRankingSourceRevisionDocument>.Filter.Eq(
                document => document.Revision,
                sourceRevision)
            & Builders<RatingRankingSourceRevisionDocument>.Filter.Eq(
                document => document.PendingMutationCount,
                0);
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            Builders<RatingRankingSourceRevisionDocument>.Update
                .Set(document => document.UnavailableMethodologyVersion, methodologyVersion.Value)
                .Set(document => document.HighestUnavailableSourceRevision, sourceRevision)
                .Set(document => document.UnavailableReasonCode, reasonCode.Trim())
                .Set(document => document.UpdatedAt, nowUtc),
            cancellationToken: cancellationToken);
        if (result.MatchedCount > 0 || sourceRevision != 0)
        {
            return;
        }

        try
        {
            await this.collection.InsertOneAsync(
                new RatingRankingSourceRevisionDocument
                {
                    Id = scopeKey.Value,
                    ScopeKey = scopeKey.Value,
                    Revision = 0,
                    UnavailableMethodologyVersion = methodologyVersion.Value,
                    HighestUnavailableSourceRevision = 0,
                    UnavailableReasonCode = reasonCode.Trim(),
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc,
                },
                cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // A concurrent mutation created or advanced the scope; its revision supersedes this marker.
        }
    }

    public async Task<RatingRankingSourceRevision?> GetAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken)
    {
        RatingRankingSourceRevisionDocument? document = await this.collection
            .Find(RatingRankingSourceRevisionMongoDefinitions.BuildScopeFilter(scopeKey))
            .FirstOrDefaultAsync(cancellationToken);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        if (document is not null
            && document.PendingMutationCount > 0
            && document.MutationLeaseExpiresAtUtc <= nowUtc)
        {
            FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument> options =
                new FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument>
                {
                    ReturnDocument = ReturnDocument.After,
                };
            RatingRankingSourceRevisionDocument? recovered = await this.collection.FindOneAndUpdateAsync(
                RatingRankingSourceRevisionMongoDefinitions.BuildPendingMutationFilter(scopeKey)
                    & Builders<RatingRankingSourceRevisionDocument>.Filter.Lte(
                        value => value.MutationLeaseExpiresAtUtc,
                        nowUtc),
                Builders<RatingRankingSourceRevisionDocument>.Update
                    .Inc(value => value.Revision, 1)
                    .Set(value => value.PendingMutationCount, 0)
                    .Unset(value => value.MutationLeaseExpiresAtUtc)
                    .Set(value => value.UpdatedAt, nowUtc),
                options,
                cancellationToken);
            document = recovered ?? await this.collection
                .Find(RatingRankingSourceRevisionMongoDefinitions.BuildScopeFilter(scopeKey))
                .FirstOrDefaultAsync(cancellationToken);
        }

        IReadOnlyCollection<RatingRankingMutationLease> expiredMutationLeases =
            (document?.MutationLeases ?? new Dictionary<string, DateTime>())
                .Where(entry => entry.Value <= nowUtc)
                .Select(entry => new RatingRankingMutationLease(scopeKey, entry.Key))
                .ToArray();
        foreach (RatingRankingMutationLease mutationLease in expiredMutationLeases)
        {
            FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument> options =
                new FindOneAndUpdateOptions<RatingRankingSourceRevisionDocument>
                {
                    ReturnDocument = ReturnDocument.After,
                };
            RatingRankingSourceRevisionDocument? recovered = await this.collection.FindOneAndUpdateAsync(
                RatingRankingSourceRevisionMongoDefinitions.BuildExpiredMutationLeaseFilter(
                    mutationLease,
                    nowUtc),
                RatingRankingSourceRevisionMongoDefinitions.BuildRecoverMutationUpdate(
                    mutationLease,
                    nowUtc),
                options,
                cancellationToken);
            document = recovered ?? document;
        }

        return document is null ? null : ToApplication(document);
    }

    private static RatingRankingSourceRevision ToApplication(
        RatingRankingSourceRevisionDocument document)
    {
        RankingScopeKey scopeKey = RankingScopeKey.Parse(document.ScopeKey);
        Dictionary<string, DateTime> mutationLeases = document.MutationLeases
            ?? new Dictionary<string, DateTime>();
        if (!string.Equals(document.Id, scopeKey.Value, StringComparison.Ordinal) ||
            document.Revision < 0 ||
            document.PendingMutationCount < 0 ||
            (document.PendingMutationCount > 0 && !document.MutationLeaseExpiresAtUtc.HasValue) ||
            mutationLeases.Keys.Any(static token => !Guid.TryParseExact(token, "N", out _)))
        {
            throw new InvalidOperationException("The persisted ranking source revision is invalid.");
        }

        int pendingMutationCount = checked(document.PendingMutationCount + mutationLeases.Count);
        IEnumerable<DateTime> mutationLeaseExpirations = mutationLeases.Values;
        if (document.PendingMutationCount > 0 && document.MutationLeaseExpiresAtUtc.HasValue)
        {
            mutationLeaseExpirations = mutationLeaseExpirations.Prepend(
                document.MutationLeaseExpiresAtUtc.Value);
        }

        DateTime? mutationLeaseExpiresAtUtc = mutationLeaseExpirations
            .Cast<DateTime?>()
            .Min();

        RatingMethodologyVersion? unavailableMethodologyVersion =
            string.IsNullOrWhiteSpace(document.UnavailableMethodologyVersion)
                ? null
                : RatingMethodologyVersion.Parse(document.UnavailableMethodologyVersion);
        return new RatingRankingSourceRevision(
            scopeKey,
            document.Revision,
            document.UpdatedAt,
            pendingMutationCount,
            mutationLeaseExpiresAtUtc,
            unavailableMethodologyVersion,
            document.HighestUnavailableSourceRevision,
            document.UnavailableReasonCode);
    }
}
