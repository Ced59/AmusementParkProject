using System.Diagnostics.CodeAnalysis;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class RankingSnapshotMongoDefinitions
{
    public static IReadOnlyCollection<BsonDocument> BuildOrphanChunkCleanupPipeline(
        RankingScopeKey scopeKey,
        string headerCollectionName,
        DateTime staleBeforeUtc,
        int maximumResultCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerCollectionName);
        if (staleBeforeUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The orphan cutoff must use UTC.", nameof(staleBeforeUtc));
        }

        if (maximumResultCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumResultCount));
        }

        return new BsonDocument[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "scopeKey", scopeKey.Value },
                { "updatedAt", new BsonDocument("$lte", staleBeforeUtc) },
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", headerCollectionName },
                { "localField", "snapshotId" },
                { "foreignField", "_id" },
                { "as", "_snapshotHeader" },
            }),
            new BsonDocument("$match", new BsonDocument(
                "_snapshotHeader",
                new BsonDocument("$size", 0))),
            new BsonDocument("$sort", new BsonDocument
            {
                { "updatedAt", 1 },
                { "_id", 1 },
            }),
            new BsonDocument("$limit", maximumResultCount),
            new BsonDocument("$project", new BsonDocument("_id", 1)),
        };
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildConfirmedOrphanChunkPruneFilter(
        RankingScopeKey scopeKey,
        IReadOnlyCollection<string> orphanDocumentIds,
        DateTime staleBeforeUtc)
    {
        ArgumentNullException.ThrowIfNull(orphanDocumentIds);
        if (staleBeforeUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The orphan cutoff must use UTC.", nameof(staleBeforeUtc));
        }

        return Builders<RankingSnapshotChunkDocument>.Filter.And(
            Builders<RankingSnapshotChunkDocument>.Filter.Eq(
                document => document.ScopeKey,
                scopeKey.Value),
            Builders<RankingSnapshotChunkDocument>.Filter.In(
                document => document.Id,
                orphanDocumentIds),
            Builders<RankingSnapshotChunkDocument>.Filter.Lte(
                document => document.UpdatedAt,
                staleBeforeUtc));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildHeaderNaturalKeyFilter(
        RankingScopeKey scopeKey,
        RatingMethodologyVersion methodologyVersion,
        long sourceRevision)
    {
        return Builders<RankingSnapshotHeaderDocument>.Filter.And(
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(document => document.ScopeKey, scopeKey.Value),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.MethodologyVersion,
                methodologyVersion.Value),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.SourceRevision,
                sourceRevision));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildFailedHeaderRestartFilter(
        RankingSnapshotId snapshotId,
        int expectedBuildAttempt)
    {
        return BuildHeaderRestartFilter(
            snapshotId,
            expectedBuildAttempt,
            RankingSnapshotStatus.Failed);
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildHeaderRestartFilter(
        RankingSnapshotId snapshotId,
        int expectedBuildAttempt,
        RankingSnapshotStatus expectedStatus)
    {
        return Builders<RankingSnapshotHeaderDocument>.Filter.And(
            BuildHeaderAttemptFilter(snapshotId, expectedBuildAttempt),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Status,
                expectedStatus));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildHeaderAttemptFilter(
        RankingSnapshotId snapshotId,
        int expectedBuildAttempt)
    {
        if (expectedBuildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedBuildAttempt));
        }

        FilterDefinitionBuilder<RankingSnapshotHeaderDocument> filters =
            Builders<RankingSnapshotHeaderDocument>.Filter;
        FilterDefinition<RankingSnapshotHeaderDocument> attemptFilter =
            filters.Eq(document => document.BuildAttempt, expectedBuildAttempt);
        if (expectedBuildAttempt == 1)
        {
            attemptFilter = filters.Or(
                attemptFilter,
                filters.Exists(document => document.BuildAttempt, false),
                filters.Eq(document => document.BuildAttempt, 0));
        }

        return filters.And(
            filters.Eq(document => document.Id, snapshotId.Value),
            attemptFilter);
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildStaleChunkAttemptFilter(
        RankingSnapshotId snapshotId,
        int chunkIndex,
        int currentBuildAttempt)
    {
        if (chunkIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkIndex));
        }

        if (currentBuildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentBuildAttempt));
        }

        FilterDefinitionBuilder<RankingSnapshotChunkDocument> filters =
            Builders<RankingSnapshotChunkDocument>.Filter;
        return filters.And(
            filters.Eq(document => document.SnapshotId, snapshotId.Value),
            filters.Eq(document => document.ChunkIndex, chunkIndex),
            filters.Or(
                filters.Exists(document => document.BuildAttempt, false),
                filters.Lt(document => document.BuildAttempt, currentBuildAttempt)));
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildChunkAttemptFilter(
        RankingSnapshotId snapshotId,
        int expectedBuildAttempt)
    {
        if (expectedBuildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedBuildAttempt));
        }

        FilterDefinitionBuilder<RankingSnapshotChunkDocument> filters =
            Builders<RankingSnapshotChunkDocument>.Filter;
        FilterDefinition<RankingSnapshotChunkDocument> attemptFilter =
            filters.Eq(document => document.BuildAttempt, expectedBuildAttempt);
        if (expectedBuildAttempt == 1)
        {
            attemptFilter = filters.Or(
                attemptFilter,
                filters.Exists(document => document.BuildAttempt, false),
                filters.Eq(document => document.BuildAttempt, 0));
        }

        return filters.And(
            filters.Eq(document => document.SnapshotId, snapshotId.Value),
            attemptFilter);
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildChunkIdentityAttemptFilter(
        RankingSnapshotId snapshotId,
        int chunkIndex,
        int expectedBuildAttempt)
    {
        if (chunkIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkIndex));
        }

        return Builders<RankingSnapshotChunkDocument>.Filter.And(
            BuildChunkAttemptFilter(snapshotId, expectedBuildAttempt),
            Builders<RankingSnapshotChunkDocument>.Filter.Eq(
                document => document.ChunkIndex,
                chunkIndex));
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildChunkAttemptAtMostFilter(
        RankingSnapshotId snapshotId,
        int maximumBuildAttempt)
    {
        if (maximumBuildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBuildAttempt));
        }

        FilterDefinitionBuilder<RankingSnapshotChunkDocument> filters =
            Builders<RankingSnapshotChunkDocument>.Filter;
        return filters.And(
            filters.Eq(document => document.SnapshotId, snapshotId.Value),
            filters.Or(
                filters.Lte(document => document.BuildAttempt, maximumBuildAttempt),
                filters.Exists(document => document.BuildAttempt, false)));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildRetentionCandidateFilter(
        RankingScopeKey scopeKey,
        IReadOnlyCollection<RankingSnapshotId> protectedSnapshotIds,
        long? highestPublishedSourceRevision,
        RatingMethodologyVersion? activeMethodologyVersion)
    {
        ArgumentNullException.ThrowIfNull(protectedSnapshotIds);
        if (highestPublishedSourceRevision.HasValue && highestPublishedSourceRevision.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(highestPublishedSourceRevision));
        }

        string[] protectedIds = protectedSnapshotIds
            .Select(static snapshotId => snapshotId.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        FilterDefinitionBuilder<RankingSnapshotHeaderDocument> filters =
            Builders<RankingSnapshotHeaderDocument>.Filter;
        List<FilterDefinition<RankingSnapshotHeaderDocument>> retentionConditions =
            new List<FilterDefinition<RankingSnapshotHeaderDocument>>
            {
                filters.In(
                    document => document.Status,
                    new[]
                    {
                        RankingSnapshotStatus.Superseded,
                        RankingSnapshotStatus.Failed,
                    }),
            };
        if (highestPublishedSourceRevision.HasValue)
        {
            FilterDefinition<RankingSnapshotHeaderDocument> staleSnapshotFilter =
                BuildStaleSnapshotRevisionFilter(
                    filters,
                    highestPublishedSourceRevision.Value,
                    activeMethodologyVersion);
            retentionConditions.Add(filters.And(
                filters.Eq(
                    document => document.Status,
                    RankingSnapshotStatus.Building),
                staleSnapshotFilter));
            retentionConditions.Add(filters.And(
                filters.Eq(
                    document => document.Status,
                    RankingSnapshotStatus.Validated),
                staleSnapshotFilter));
        }

        FilterDefinition<RankingSnapshotHeaderDocument> filter = filters.And(
            filters.Eq(document => document.ScopeKey, scopeKey.Value),
            filters.Or(retentionConditions));
        return protectedIds.Length == 0
            ? filter
            : filters.And(filter, filters.Nin(document => document.Id, protectedIds));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildSupersededHeaderPruneFilter(
        RankingSnapshotId snapshotId)
    {
        return Builders<RankingSnapshotHeaderDocument>.Filter.And(
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Id,
                snapshotId.Value),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Status,
                RankingSnapshotStatus.Superseded));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildStaleBuildingHeaderPruneFilter(
        RankingSnapshotId snapshotId,
        int expectedBuildAttempt,
        long highestPublishedSourceRevision,
        RatingMethodologyVersion? activeMethodologyVersion)
    {
        if (highestPublishedSourceRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(highestPublishedSourceRevision));
        }

        return Builders<RankingSnapshotHeaderDocument>.Filter.And(
            BuildHeaderAttemptFilter(snapshotId, expectedBuildAttempt),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Status,
                RankingSnapshotStatus.Building),
            BuildStaleSnapshotRevisionFilter(
                Builders<RankingSnapshotHeaderDocument>.Filter,
                highestPublishedSourceRevision,
                activeMethodologyVersion));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument>
        BuildOrphanedCurrentHeadersReconciliationFilter(RankingPublicationPointer livePointer)
    {
        ArgumentNullException.ThrowIfNull(livePointer);
        FilterDefinitionBuilder<RankingSnapshotHeaderDocument> filters =
            Builders<RankingSnapshotHeaderDocument>.Filter;
        List<string> protectedSnapshotIds = new List<string>
        {
            livePointer.CurrentSnapshotId.Value,
        };
        if (livePointer.PreviousSnapshotId.HasValue &&
            livePointer.PreviousSnapshotId.Value != livePointer.CurrentSnapshotId)
        {
            protectedSnapshotIds.Add(livePointer.PreviousSnapshotId.Value.Value);
        }

        return filters.And(
            filters.Eq(document => document.ScopeKey, livePointer.ScopeKey.Value),
            filters.Eq(document => document.Status, RankingSnapshotStatus.Current),
            filters.Nin(document => document.Id, protectedSnapshotIds),
            filters.Or(
                filters.Exists(document => document.ReconciledPointerVersion, false),
                filters.Eq(document => document.ReconciledPointerVersion, null),
                filters.Lt(document => document.ReconciledPointerVersion, livePointer.Version)));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildStaleValidatedHeaderPruneFilter(
        RankingSnapshotId snapshotId,
        long highestPublishedSourceRevision,
        RatingMethodologyVersion? activeMethodologyVersion)
    {
        if (highestPublishedSourceRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(highestPublishedSourceRevision));
        }

        return Builders<RankingSnapshotHeaderDocument>.Filter.And(
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Id,
                snapshotId.Value),
            Builders<RankingSnapshotHeaderDocument>.Filter.Eq(
                document => document.Status,
                RankingSnapshotStatus.Validated),
            BuildStaleSnapshotRevisionFilter(
                Builders<RankingSnapshotHeaderDocument>.Filter,
                highestPublishedSourceRevision,
                activeMethodologyVersion));
    }

    private static FilterDefinition<RankingSnapshotHeaderDocument> BuildStaleSnapshotRevisionFilter(
        FilterDefinitionBuilder<RankingSnapshotHeaderDocument> filters,
        long highestPublishedSourceRevision,
        RatingMethodologyVersion? activeMethodologyVersion)
    {
        FilterDefinition<RankingSnapshotHeaderDocument> olderRevision = filters.Lt(
            document => document.SourceRevision,
            highestPublishedSourceRevision);
        if (!activeMethodologyVersion.HasValue)
        {
            return filters.Lte(
                document => document.SourceRevision,
                highestPublishedSourceRevision);
        }

        return filters.Or(
            olderRevision,
            filters.And(
                filters.Eq(
                    document => document.SourceRevision,
                    highestPublishedSourceRevision),
                filters.Ne(
                    document => document.MethodologyVersion,
                    activeMethodologyVersion.Value.Value)));
    }

    public static int NormalizeBuildAttempt(int buildAttempt)
    {
        return Math.Max(1, buildAttempt);
    }

    public static FilterDefinition<RankingPublicationPointerDocument> BuildPointerVersionFilter(
        RankingScopeKey scopeKey,
        long expectedVersion)
    {
        return Builders<RankingPublicationPointerDocument>.Filter.And(
            Builders<RankingPublicationPointerDocument>.Filter.Eq(document => document.ScopeKey, scopeKey.Value),
            Builders<RankingPublicationPointerDocument>.Filter.Eq(document => document.Version, expectedVersion));
    }

    public static FilterDefinition<RankingPublicationPointerDocument> BuildLivePointerFilter(
        RankingPublicationPointer pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        return Builders<RankingPublicationPointerDocument>.Filter.And(
            BuildPointerVersionFilter(pointer.ScopeKey, pointer.Version),
            Builders<RankingPublicationPointerDocument>.Filter.Eq(
                document => document.CurrentSnapshotId,
                pointer.CurrentSnapshotId.Value));
    }

    public static FilterDefinition<RankingSnapshotHeaderDocument> BuildHeaderReconciliationFilter(
        RankingSnapshotId snapshotId,
        long pointerVersion)
    {
        if (pointerVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pointerVersion));
        }

        FilterDefinitionBuilder<RankingSnapshotHeaderDocument> filters =
            Builders<RankingSnapshotHeaderDocument>.Filter;
        return filters.And(
            filters.Eq(document => document.Id, snapshotId.Value),
            filters.Nin(
                document => document.Status,
                new[] { RankingSnapshotStatus.Building, RankingSnapshotStatus.Failed }),
            filters.Or(
                filters.Exists(document => document.ReconciledPointerVersion, false),
                filters.Eq(document => document.ReconciledPointerVersion, null),
                filters.Lte(document => document.ReconciledPointerVersion, pointerVersion)));
    }

    public static FilterDefinition<RankingSnapshotChunkDocument> BuildPageChunkFilter(
        RankingSnapshotId snapshotId,
        int firstChunkIndex,
        int lastChunkIndex)
    {
        if (firstChunkIndex < 0 || lastChunkIndex < firstChunkIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(firstChunkIndex));
        }

        return Builders<RankingSnapshotChunkDocument>.Filter.And(
            Builders<RankingSnapshotChunkDocument>.Filter.Eq(
                document => document.SnapshotId,
                snapshotId.Value),
            Builders<RankingSnapshotChunkDocument>.Filter.Gte(
                document => document.ChunkIndex,
                firstChunkIndex),
            Builders<RankingSnapshotChunkDocument>.Filter.Lte(
                document => document.ChunkIndex,
                lastChunkIndex));
    }

    public static bool IsStale(RankingPublicationPointer pointer, RankingSnapshotHeader candidate)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        ArgumentNullException.ThrowIfNull(candidate);
        return pointer.ScopeKey == candidate.ScopeKey &&
            (pointer.HighestPublishedSourceRevision > candidate.SourceRevision ||
                (pointer.HighestPublishedSourceRevision == candidate.SourceRevision &&
                    pointer.MethodologyVersion == candidate.MethodologyVersion &&
                    candidate.GeneratedAtUtc <= pointer.UpdatedAtUtc));
    }

    public static bool IsPublishableForScope(
        RankingSnapshotHeader snapshot,
        RankingScopeDefinition scope)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(scope);
        bool hasValidatedLifecycle = snapshot.Status is RankingSnapshotStatus.Validated
            or RankingSnapshotStatus.Current
            or RankingSnapshotStatus.Superseded;
        return hasValidatedLifecycle &&
            snapshot.ScopeKey == scope.Key &&
            snapshot.MethodologyVersion == scope.MethodologyVersion &&
            scope.EvaluatePublication(snapshot.EligibleEntryCount).IsEligible;
    }

    public static DateTime ResolvePublishedAt(
        RankingSnapshotHeader snapshot,
        DateTime fallbackPublishedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (fallbackPublishedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The fallback publication timestamp must use UTC.",
                nameof(fallbackPublishedAtUtc));
        }

        return snapshot.PublishedAtUtc ?? fallbackPublishedAtUtc;
    }
}
