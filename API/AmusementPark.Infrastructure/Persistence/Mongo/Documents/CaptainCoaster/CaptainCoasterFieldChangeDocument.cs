using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;

public sealed class CaptainCoasterFieldChangeDocument
{
    [BsonElement("field")]
    public string Field { get; set; } = string.Empty;

    [BsonElement("localValue")]
    [BsonIgnoreIfNull]
    public string? LocalValue { get; set; }

    [BsonElement("externalValue")]
    [BsonIgnoreIfNull]
    public string? ExternalValue { get; set; }

    [BsonElement("isDifferent")]
    public bool IsDifferent { get; set; }
}
