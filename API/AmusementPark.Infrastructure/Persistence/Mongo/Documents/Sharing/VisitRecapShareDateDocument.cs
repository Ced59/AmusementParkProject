using AmusementPark.Core.Domain.Sharing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class VisitRecapShareDateDocument
{
    [BsonElement("year")]
    public int Year { get; set; }

    [BsonElement("month")]
    [BsonIgnoreIfNull]
    public int? Month { get; set; }

    [BsonElement("day")]
    [BsonIgnoreIfNull]
    public int? Day { get; set; }

    [BsonElement("precision")]
    [BsonRepresentation(BsonType.String)]
    public ShareDatePrecision Precision { get; set; }

    [BsonElement("isApproximate")]
    public bool IsApproximate { get; set; }
}
