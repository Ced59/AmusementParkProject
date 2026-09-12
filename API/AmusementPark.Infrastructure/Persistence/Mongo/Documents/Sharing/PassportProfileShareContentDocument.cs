using AmusementPark.Core.Domain.Sharing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class PassportProfileShareContentDocument
{
    [BsonElement("displayName")]
    [BsonIgnoreIfNull]
    public string? DisplayName { get; set; }

    [BsonElement("avatarUrl")]
    [BsonIgnoreIfNull]
    public string? AvatarUrl { get; set; }

    [BsonElement("publicCaption")]
    [BsonIgnoreIfNull]
    public string? PublicCaption { get; set; }

    [BsonElement("visibility")]
    [BsonRepresentation(BsonType.String)]
    public ShareVisibility Visibility { get; set; } = ShareVisibility.Unlisted;

    [BsonElement("allowsComparisons")]
    public bool AllowsComparisons { get; set; }

    [BsonElement("parkCount")]
    [BsonIgnoreIfNull]
    public long? ParkCount { get; set; }

    [BsonElement("visitCount")]
    [BsonIgnoreIfNull]
    public long? VisitCount { get; set; }

    [BsonElement("totalRideCount")]
    [BsonIgnoreIfNull]
    public long? TotalRideCount { get; set; }

    [BsonElement("distinctItemCount")]
    [BsonIgnoreIfNull]
    public long? DistinctItemCount { get; set; }

    [BsonElement("visitRatings")]
    [BsonIgnoreIfNull]
    public PassportProfileShareRatingSummaryDocument? VisitRatings { get; set; }

    [BsonElement("rideRatings")]
    [BsonIgnoreIfNull]
    public PassportProfileShareRatingSummaryDocument? RideRatings { get; set; }

    [BsonElement("countries")]
    public List<PassportProfileShareCountryDocument> Countries { get; set; } = new();

    [BsonElement("years")]
    public List<PassportProfileShareYearDocument> Years { get; set; } = new();

    [BsonElement("parks")]
    public List<PassportProfileShareParkDocument> Parks { get; set; } = new();

    [BsonElement("personalRanking")]
    public List<PassportProfileShareRatingDocument> PersonalRanking { get; set; } = new();

    [BsonElement("missedItems")]
    public List<PassportProfileShareMissedItemDocument> MissedItems { get; set; } = new();

    [BsonElement("hasIncompleteCatalog")]
    public bool HasIncompleteCatalog { get; set; }

    [BsonElement("calculationVersion")]
    public string CalculationVersion { get; set; } = string.Empty;

    [BsonElement("isEmpty")]
    public bool IsEmpty { get; set; }
}
