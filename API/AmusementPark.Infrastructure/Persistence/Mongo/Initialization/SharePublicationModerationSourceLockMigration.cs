using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

internal sealed class SharePublicationModerationSourceLockMigration
{
    private static readonly SharePublicationStatus[] ActiveStatuses =
    {
        SharePublicationStatus.Draft,
        SharePublicationStatus.Published,
        SharePublicationStatus.NeedsReview,
    };

    private readonly IMongoCollection<SharePublicationDocument> collection;

    public SharePublicationModerationSourceLockMigration(
        IMongoCollection<SharePublicationDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if (await this.IndexExistsAsync(
                SharePublicationMongoDefinitions.ActiveOwnerSourceUniqueIndexName,
                cancellationToken))
        {
            return;
        }

        FilterDefinition<SharePublicationDocument> candidateFilter =
            Builders<SharePublicationDocument>.Filter.In(
                static document => document.Status,
                ActiveStatuses)
            | Builders<SharePublicationDocument>.Filter.Exists(
                "moderationSuspensionReportIds.0",
                true);
        List<SharePublicationDocument> candidates = await this.collection
            .Find(candidateFilter)
            .ToListAsync(cancellationToken);
        IEnumerable<IGrouping<(string OwnerUserId, SharePublicationType Type, string SourceScopeKey),
            SharePublicationDocument>> sourceGroups = candidates.GroupBy(static document =>
                (document.OwnerUserId, document.Type, document.SourceScopeKey));
        foreach (IGrouping<(string OwnerUserId, SharePublicationType Type, string SourceScopeKey),
                     SharePublicationDocument> sourceGroup in sourceGroups)
        {
            await this.ConsolidateSourceBlockAsync(sourceGroup, cancellationToken);
        }

        await this.DropIndexIfExistsAsync(
            SharePublicationMongoDefinitions.LegacyActiveOwnerSourceUniqueIndexName,
            cancellationToken);
    }

    private async Task ConsolidateSourceBlockAsync(
        IEnumerable<SharePublicationDocument> sourceDocuments,
        CancellationToken cancellationToken)
    {
        SharePublicationDocument[] documents = sourceDocuments.ToArray();
        List<string> reportIds = documents
            .SelectMany(static document => document.ModerationSuspensionReportIds)
            .Where(static reportId => !string.IsNullOrWhiteSpace(reportId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static reportId => reportId, StringComparer.Ordinal)
            .ToList();
        if (reportIds.Count == 0)
        {
            return;
        }

        SharePublicationDocument anchor = documents
            .Where(static document => ActiveStatuses.Contains(document.Status))
            .OrderByDescending(static document => document.UpdatedAt)
            .ThenByDescending(static document => document.CreatedAt)
            .ThenByDescending(static document => document.Id, StringComparer.Ordinal)
            .FirstOrDefault()
            ?? documents
                .OrderByDescending(static document => document.UpdatedAt)
                .ThenByDescending(static document => document.CreatedAt)
                .ThenByDescending(static document => document.Id, StringComparer.Ordinal)
                .First();

        foreach (SharePublicationDocument document in documents)
        {
            List<string> desiredReportIds = string.Equals(
                    document.Id,
                    anchor.Id,
                    StringComparison.Ordinal)
                ? reportIds
                : new List<string>();
            if (document.ModerationSuspensionReportIds.SequenceEqual(
                    desiredReportIds,
                    StringComparer.Ordinal))
            {
                continue;
            }

            if (document.Version == long.MaxValue)
            {
                throw new InvalidOperationException(
                    "A share publication source block cannot be migrated after its version reached the maximum value.");
            }

            DateTime currentUtc = DateTime.UtcNow;
            DateTime updatedAtUtc = currentUtc < document.UpdatedAt
                ? document.UpdatedAt
                : currentUtc;
            FilterDefinition<SharePublicationDocument> updateFilter =
                Builders<SharePublicationDocument>.Filter.Eq(
                    static candidate => candidate.Id,
                    document.Id)
                & Builders<SharePublicationDocument>.Filter.Eq(
                    static candidate => candidate.Version,
                    document.Version);
            UpdateDefinition<SharePublicationDocument> update =
                Builders<SharePublicationDocument>.Update
                    .Set(
                        static candidate => candidate.ModerationSuspensionReportIds,
                        desiredReportIds)
                    .Set(static candidate => candidate.UpdatedAt, updatedAtUtc)
                    .Inc(static candidate => candidate.Version, 1);
            UpdateResult result = await this.collection.UpdateOneAsync(
                updateFilter,
                update,
                cancellationToken: cancellationToken);
            if (result.MatchedCount != 1)
            {
                throw new InvalidOperationException(
                    "A share publication changed while its moderation source block was being migrated.");
            }
        }
    }

    private async Task<bool> IndexExistsAsync(
        string indexName,
        CancellationToken cancellationToken)
    {
        using IAsyncCursor<BsonDocument> cursor = await this.collection.Indexes.ListAsync(
            cancellationToken);
        List<BsonDocument> indexes = await cursor.ToListAsync(cancellationToken);
        return indexes.Any(index => index.TryGetValue("name", out BsonValue? name)
            && name.IsString
            && string.Equals(name.AsString, indexName, StringComparison.Ordinal));
    }

    private async Task DropIndexIfExistsAsync(
        string indexName,
        CancellationToken cancellationToken)
    {
        if (await this.IndexExistsAsync(indexName, cancellationToken))
        {
            await this.collection.Indexes.DropOneAsync(indexName, cancellationToken);
        }
    }
}
