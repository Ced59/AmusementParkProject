using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class VisitRecapShareContentDocument
{
    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("parkName")]
    [BsonIgnoreIfNull]
    public string? ParkName { get; set; }

    [BsonElement("date")]
    [BsonIgnoreIfNull]
    public VisitRecapShareDateDocument? Date { get; set; }

    [BsonElement("distinctItemCount")]
    [BsonIgnoreIfNull]
    public int? DistinctItemCount { get; set; }

    [BsonElement("totalRideCount")]
    [BsonIgnoreIfNull]
    public int? TotalRideCount { get; set; }

    [BsonElement("categories")]
    public List<string> Categories { get; set; } = new();

    [BsonElement("parkRating")]
    [BsonIgnoreIfNull]
    public double? ParkRating { get; set; }

    [BsonElement("topRatedItem")]
    [BsonIgnoreIfNull]
    public VisitRecapShareHighlightDocument? TopRatedItem { get; set; }

    [BsonElement("mostRepeatedItem")]
    [BsonIgnoreIfNull]
    public VisitRecapShareHighlightDocument? MostRepeatedItem { get; set; }

    [BsonElement("items")]
    public List<VisitRecapShareItemDocument> Items { get; set; } = new();

    [BsonElement("publicCaption")]
    [BsonIgnoreIfNull]
    public string? PublicCaption { get; set; }

    [BsonElement("hasHiddenDate")]
    public bool HasHiddenDate { get; set; }

    [BsonElement("hasIncompleteRatings")]
    public bool HasIncompleteRatings { get; set; }

    [BsonElement("hasIncompleteItems")]
    public bool HasIncompleteItems { get; set; }
}
