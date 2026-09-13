using AmusementPark.Core.Domain.Sharing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

public sealed class ProfileComparisonCalculationDocument
{
    [BsonElement("creatorDisplayName")]
    [BsonIgnoreIfNull]
    public string? CreatorDisplayName { get; set; }

    [BsonElement("acceptorDisplayName")]
    [BsonIgnoreIfNull]
    public string? AcceptorDisplayName { get; set; }

    [BsonElement("categories")]
    [BsonRepresentation(BsonType.String)]
    public List<ProfileComparisonCategory> Categories { get; set; } = new();

    [BsonElement("parks")]
    public List<ProfileComparisonParkDocument> Parks { get; set; } = new();

    [BsonElement("ratings")]
    public List<ProfileComparisonRatingDocument> Ratings { get; set; } = new();

    [BsonElement("years")]
    public List<ProfileComparisonYearDocument> Years { get; set; } = new();

    [BsonElement("missedItems")]
    public List<ProfileComparisonMissedItemDocument> MissedItems { get; set; } = new();

    [BsonElement("commonRatingCount")]
    public int CommonRatingCount { get; set; }

    [BsonElement("minimumRatingsForCorrelation")]
    public int MinimumRatingsForCorrelation { get; set; }

    [BsonElement("ratingCorrelation")]
    [BsonIgnoreIfNull]
    public double? RatingCorrelation { get; set; }

    [BsonElement("hasIncompleteCatalog")]
    public bool HasIncompleteCatalog { get; set; }

    [BsonElement("calculationVersion")]
    public string CalculationVersion { get; set; } = string.Empty;
}
