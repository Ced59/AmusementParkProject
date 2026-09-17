using AmusementPark.Core.Domain.Trips;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripDateProposalDocument
{
    [BsonElement("kind")]
    [BsonRepresentation(BsonType.String)]
    public TripDateProposalKind Kind { get; set; }

    [BsonElement("startDate")]
    [BsonIgnoreIfNull]
    public string? StartDate { get; set; }

    [BsonElement("endDate")]
    [BsonIgnoreIfNull]
    public string? EndDate { get; set; }

    [BsonElement("candidateDates")]
    public List<string> CandidateDates { get; set; } = new();
}
