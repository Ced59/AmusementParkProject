using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.TechnicalPages;

public sealed class TechnicalContentTableDocument
{
    [BsonElement("headers")]
    public List<TechnicalContentTableCellDocument> Headers { get; set; } = new();

    [BsonElement("rows")]
    public List<TechnicalContentTableRowDocument> Rows { get; set; } = new();
}
