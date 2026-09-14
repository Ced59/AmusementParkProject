using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;

[BsonIgnoreExtraElements]
public sealed class ParkFitGroupProfileDocument : MongoDocumentBase
{
    [BsonElement("ownerUserId")]
    public string OwnerUserId { get; set; } = string.Empty;

    [BsonElement("alias")]
    public string Alias { get; set; } = string.Empty;

    [BsonElement("normalizedAlias")]
    public string NormalizedAlias { get; set; } = string.Empty;

    [BsonElement("heightCentimeters")]
    [BsonIgnoreIfNull]
    public int? HeightCentimeters { get; set; }

    [BsonElement("ageYears")]
    [BsonIgnoreIfNull]
    public int? AgeYears { get; set; }

    [BsonElement("canBeAccompanied")]
    public bool CanBeAccompanied { get; set; }

    [BsonElement("companionAgeYears")]
    [BsonIgnoreIfNull]
    public int? CompanionAgeYears { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
