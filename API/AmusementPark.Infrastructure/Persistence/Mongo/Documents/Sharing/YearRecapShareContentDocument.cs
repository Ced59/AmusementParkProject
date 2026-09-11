using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class YearRecapShareContentDocument
{
    [BsonElement("year")]
    public int Year { get; set; }

    [BsonElement("parkCount")]
    [BsonIgnoreIfNull]
    public long? ParkCount { get; set; }

    [BsonElement("visitCount")]
    public long VisitCount { get; set; }

    [BsonElement("approximateVisitCount")]
    public long ApproximateVisitCount { get; set; }

    [BsonElement("approximateVisitRate")]
    public double ApproximateVisitRate { get; set; }

    [BsonElement("totalRideCount")]
    [BsonIgnoreIfNull]
    public long? TotalRideCount { get; set; }

    [BsonElement("distinctItemCount")]
    [BsonIgnoreIfNull]
    public long? DistinctItemCount { get; set; }

    [BsonElement("missedItemCount")]
    [BsonIgnoreIfNull]
    public long? MissedItemCount { get; set; }

    [BsonElement("categories")]
    public List<string> Categories { get; set; } = new();

    [BsonElement("parkRatings")]
    [BsonIgnoreIfNull]
    public YearRecapShareRatingSummaryDocument? ParkRatings { get; set; }

    [BsonElement("rideRatings")]
    [BsonIgnoreIfNull]
    public YearRecapShareRatingSummaryDocument? RideRatings { get; set; }

    [BsonElement("mostVisitedParks")]
    public List<YearRecapShareParkDocument> MostVisitedParks { get; set; } = new();

    [BsonElement("mostRepeatedItem")]
    [BsonIgnoreIfNull]
    public YearRecapShareHighlightDocument? MostRepeatedItem { get; set; }

    [BsonElement("topRatedItem")]
    [BsonIgnoreIfNull]
    public YearRecapShareHighlightDocument? TopRatedItem { get; set; }

    [BsonElement("ratingEvolution")]
    [BsonIgnoreIfNull]
    public YearRecapShareTrendDocument? RatingEvolution { get; set; }

    [BsonElement("nowClosedItems")]
    public List<YearRecapShareHighlightDocument> NowClosedItems { get; set; } = new();

    [BsonElement("publicCaption")]
    [BsonIgnoreIfNull]
    public string? PublicCaption { get; set; }

    [BsonElement("hasIncompleteCatalog")]
    public bool HasIncompleteCatalog { get; set; }

    [BsonElement("calculationVersion")]
    public string CalculationVersion { get; set; } = string.Empty;

    [BsonElement("isEmpty")]
    public bool IsEmpty { get; set; }
}
