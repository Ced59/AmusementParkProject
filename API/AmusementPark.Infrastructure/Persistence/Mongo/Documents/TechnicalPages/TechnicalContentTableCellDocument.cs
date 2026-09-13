using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.TechnicalPages;

public sealed class TechnicalContentTableCellDocument
{
    [BsonElement("texts")]
    public List<LocalizedTextDocument> Texts { get; set; } = new();
}
