using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.TechnicalPages;

public sealed class TechnicalContentMetricDocument
{
    [BsonElement("label")]
    public List<LocalizedTextDocument> Label { get; set; } = new();

    [BsonElement("value")]
    public List<LocalizedTextDocument> Value { get; set; } = new();

    [BsonElement("helpText")]
    public List<LocalizedTextDocument> HelpText { get; set; } = new();
}
