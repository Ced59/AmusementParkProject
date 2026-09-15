using AmusementPark.Core.Domain.FactualEvents;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;

[BsonIgnoreExtraElements]
public sealed class FactValueDocument
{
    [BsonElement("kind")]
    public FactValueKind Kind { get; set; }

    [BsonElement("canonicalValue")]
    public string CanonicalValue { get; set; } = string.Empty;

    [BsonElement("unitCode")]
    [BsonIgnoreIfNull]
    public string? UnitCode { get; set; }
}
