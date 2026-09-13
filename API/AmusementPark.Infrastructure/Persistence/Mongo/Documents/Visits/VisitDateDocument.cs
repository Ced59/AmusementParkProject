using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class VisitDateDocument
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
    public VisitDatePrecision Precision { get; set; }

    [BsonElement("isApproximate")]
    public bool IsApproximate { get; set; }
}
