using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;

[BsonIgnoreExtraElements]
public sealed class RankingSnapshotChunkDocument : MongoDocumentBase
{
    [BsonElement("scopeKey")]
    public string ScopeKey { get; set; } = string.Empty;

    [BsonElement("snapshotId")]
    public string SnapshotId { get; set; } = string.Empty;

    [BsonElement("chunkIndex")]
    public int ChunkIndex { get; set; }

    [BsonElement("firstRank")]
    public int FirstRank { get; set; }

    [BsonElement("lastRank")]
    public int LastRank { get; set; }

    [BsonElement("firstPosition")]
    public int FirstPosition { get; set; }

    [BsonElement("lastPosition")]
    public int LastPosition { get; set; }

    [BsonElement("entryCount")]
    public int EntryCount { get; set; }

    [BsonElement("buildAttempt")]
    public int BuildAttempt { get; set; }

    [BsonElement("checksum")]
    public string Checksum { get; set; } = string.Empty;

    [BsonElement("entries")]
    public List<RankingSnapshotEntryDocument> Entries { get; set; } = new List<RankingSnapshotEntryDocument>();
}
