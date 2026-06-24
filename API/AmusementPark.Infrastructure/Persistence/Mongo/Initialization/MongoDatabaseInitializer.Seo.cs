using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Seo;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeSeoSitemapIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<SeoSitemapSnapshotSectionChunkDocument> sectionChunksCollection =
            this.database.GetCollection<SeoSitemapSnapshotSectionChunkDocument>(this.settings.SeoSitemapSnapshotSectionsCollectionName);

        List<CreateIndexModel<SeoSitemapSnapshotSectionChunkDocument>> sectionChunkIndexes =
            new List<CreateIndexModel<SeoSitemapSnapshotSectionChunkDocument>>
            {
                new CreateIndexModel<SeoSitemapSnapshotSectionChunkDocument>(
                    Builders<SeoSitemapSnapshotSectionChunkDocument>.IndexKeys
                        .Ascending(item => item.SnapshotId)
                        .Ascending(item => item.StorageId)
                        .Ascending(item => item.SectionKey)
                        .Ascending(item => item.ChunkIndex),
                    new CreateIndexOptions { Name = "idx_seo_sitemap_section_chunks_lookup", Unique = true }),
            };

        await sectionChunksCollection.Indexes.CreateManyAsync(sectionChunkIndexes, cancellationToken: cancellationToken);
    }
}
