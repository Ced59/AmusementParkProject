using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeRankingSnapshotIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<RankingSnapshotHeaderDocument> headers = this.database.GetCollection<RankingSnapshotHeaderDocument>(
            this.settings.RatingRankingSnapshotHeadersCollectionName);
        IMongoCollection<RankingSnapshotChunkDocument> chunks = this.database.GetCollection<RankingSnapshotChunkDocument>(
            this.settings.RatingRankingSnapshotChunksCollectionName);
        IMongoCollection<RankingPublicationPointerDocument> pointers = this.database.GetCollection<RankingPublicationPointerDocument>(
            this.settings.RatingRankingPublicationPointersCollectionName);

        await headers.Indexes.CreateManyAsync(BuildRankingSnapshotHeaderIndexes(), cancellationToken);
        await chunks.Indexes.CreateManyAsync(BuildRankingSnapshotChunkIndexes(), cancellationToken);
        await pointers.Indexes.CreateManyAsync(BuildRankingPublicationPointerIndexes(), cancellationToken);
    }

    internal static IReadOnlyCollection<CreateIndexModel<RankingSnapshotHeaderDocument>> BuildRankingSnapshotHeaderIndexes()
    {
        return new List<CreateIndexModel<RankingSnapshotHeaderDocument>>
        {
            new CreateIndexModel<RankingSnapshotHeaderDocument>(
                Builders<RankingSnapshotHeaderDocument>.IndexKeys
                    .Ascending(document => document.ScopeKey)
                    .Ascending(document => document.MethodologyVersion)
                    .Ascending(document => document.SourceRevision),
                new CreateIndexOptions { Name = "idx_ranking_snapshot_header_source_unique", Unique = true }),
            new CreateIndexModel<RankingSnapshotHeaderDocument>(
                Builders<RankingSnapshotHeaderDocument>.IndexKeys
                    .Ascending(document => document.ScopeKey)
                    .Ascending(document => document.Status)
                    .Descending(document => document.GeneratedAtUtc),
                new CreateIndexOptions { Name = "idx_ranking_snapshot_header_scope_status" }),
        };
    }

    internal static IReadOnlyCollection<CreateIndexModel<RankingSnapshotChunkDocument>> BuildRankingSnapshotChunkIndexes()
    {
        return new List<CreateIndexModel<RankingSnapshotChunkDocument>>
        {
            new CreateIndexModel<RankingSnapshotChunkDocument>(
                Builders<RankingSnapshotChunkDocument>.IndexKeys
                    .Ascending(document => document.SnapshotId)
                    .Ascending(document => document.ChunkIndex),
                new CreateIndexOptions { Name = "idx_ranking_snapshot_chunk_index_unique", Unique = true }),
            new CreateIndexModel<RankingSnapshotChunkDocument>(
                Builders<RankingSnapshotChunkDocument>.IndexKeys
                    .Ascending(document => document.SnapshotId)
                    .Ascending(document => document.FirstRank)
                    .Ascending(document => document.LastRank),
                new CreateIndexOptions { Name = "idx_ranking_snapshot_chunk_rank_range" }),
            new CreateIndexModel<RankingSnapshotChunkDocument>(
                Builders<RankingSnapshotChunkDocument>.IndexKeys
                    .Ascending(document => document.ScopeKey)
                    .Ascending(document => document.UpdatedAt),
                new CreateIndexOptions { Name = "idx_ranking_snapshot_chunk_orphan_cleanup" }),
        };
    }

    internal static IReadOnlyCollection<CreateIndexModel<RankingPublicationPointerDocument>> BuildRankingPublicationPointerIndexes()
    {
        return new List<CreateIndexModel<RankingPublicationPointerDocument>>
        {
            new CreateIndexModel<RankingPublicationPointerDocument>(
                Builders<RankingPublicationPointerDocument>.IndexKeys.Ascending(document => document.ScopeKey),
                new CreateIndexOptions { Name = "idx_ranking_publication_pointer_scope_unique", Unique = true }),
        };
    }
}
