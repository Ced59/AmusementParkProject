using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.TechnicalPages;

public sealed class TechnicalContentLinkDocument
{
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("label")]
    public List<LocalizedTextDocument> Label { get; set; } = new();
}
