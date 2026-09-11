using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class YearRecapShareRatingSummaryDocument
{
    [BsonElement("ratedCount")]
    public long RatedCount { get; set; }

    [BsonElement("eligibleCount")]
    public long EligibleCount { get; set; }

    [BsonElement("average")]
    [BsonIgnoreIfNull]
    public double? Average { get; set; }
}
