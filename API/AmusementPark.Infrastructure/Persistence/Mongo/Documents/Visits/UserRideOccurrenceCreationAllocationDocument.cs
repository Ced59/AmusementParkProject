using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceCreationAllocationDocument
{
    [BsonElement("index")]
    public int Index { get; set; }

    [BsonElement("occurrenceId")]
    public string OccurrenceId { get; set; } = string.Empty;

    [BsonElement("sortPosition")]
    public long SortPosition { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }

    [BsonElement("creationSnapshot")]
    public UserRideOccurrenceCreationSnapshotDocument CreationSnapshot { get; set; } =
        new UserRideOccurrenceCreationSnapshotDocument();
}
