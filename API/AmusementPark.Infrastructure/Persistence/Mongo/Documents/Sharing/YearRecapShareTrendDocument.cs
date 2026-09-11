using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class YearRecapShareTrendDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("kind")]
    public string Kind { get; set; } = string.Empty;

    [BsonElement("firstWindowRatingCount")]
    public long FirstWindowRatingCount { get; set; }

    [BsonElement("lastWindowRatingCount")]
    public long LastWindowRatingCount { get; set; }

    [BsonElement("firstWindowAverage")]
    public double FirstWindowAverage { get; set; }

    [BsonElement("lastWindowAverage")]
    public double LastWindowAverage { get; set; }

    [BsonElement("delta")]
    public double Delta { get; set; }
}
