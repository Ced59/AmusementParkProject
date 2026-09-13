using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Seo;

public sealed class SeoSitemapSnapshotSectionChunkDocument : MongoDocumentBase
{
    [BsonElement("snapshotId")]
    public string SnapshotId { get; set; } = string.Empty;

    [BsonElement("storageId")]
    public string StorageId { get; set; } = string.Empty;

    [BsonElement("sectionKey")]
    public string SectionKey { get; set; } = string.Empty;

    [BsonElement("chunkIndex")]
    public int ChunkIndex { get; set; }

    [BsonElement("chunkCount")]
    public int ChunkCount { get; set; }

    [BsonElement("xmlChunk")]
    public string XmlChunk { get; set; } = string.Empty;
}
