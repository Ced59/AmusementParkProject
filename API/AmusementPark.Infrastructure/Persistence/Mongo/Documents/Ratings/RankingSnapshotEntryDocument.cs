using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;

public sealed class RankingSnapshotEntryDocument
{
    [BsonElement("position")]
    public int Position { get; set; }

    [BsonElement("rank")]
    public int Rank { get; set; }

    [BsonElement("targetType")]
    [BsonRepresentation(BsonType.String)]
    public RatingTargetType TargetType { get; set; }

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("parkItemCategory")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public ParkItemCategory? ParkItemCategory { get; set; }

    [BsonElement("score")]
    public double Score { get; set; }

    [BsonElement("evidenceLevel")]
    [BsonRepresentation(BsonType.String)]
    public RankingEvidenceLevel EvidenceLevel { get; set; }

    [BsonElement("uniqueContributorCount")]
    public int UniqueContributorCount { get; set; }

    [BsonElement("ratingObservationCount")]
    public int RatingObservationCount { get; set; }

    [BsonElement("directParkContributorCount")]
    [BsonIgnoreIfNull]
    public int? DirectParkContributorCount { get; set; }

    [BsonElement("itemContributorCount")]
    [BsonIgnoreIfNull]
    public int? ItemContributorCount { get; set; }

    [BsonElement("eligibleItemCount")]
    [BsonIgnoreIfNull]
    public int? EligibleItemCount { get; set; }

    [BsonElement("eligibleCategoryCount")]
    [BsonIgnoreIfNull]
    public int? EligibleCategoryCount { get; set; }

    [BsonElement("publicItemCategoryCount")]
    [BsonIgnoreIfNull]
    public int? PublicItemCategoryCount { get; set; }

    [BsonElement("isSingleCategoryParkException")]
    [BsonIgnoreIfNull]
    public bool? IsSingleCategoryParkException { get; set; }

    [BsonElement("nextContributorThreshold")]
    [BsonIgnoreIfNull]
    public int? NextContributorThreshold { get; set; }
}
