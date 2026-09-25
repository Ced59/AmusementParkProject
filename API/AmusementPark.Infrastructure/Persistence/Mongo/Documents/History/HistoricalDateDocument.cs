using AmusementPark.Core.Domain.History;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

public sealed class HistoricalDateDocument
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
    public HistoryDatePrecision Precision { get; set; }

    [BsonElement("isApproximate")]
    public bool IsApproximate { get; set; }

    [BsonElement("qualifier")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public DateQualifier? Qualifier { get; set; }
}
