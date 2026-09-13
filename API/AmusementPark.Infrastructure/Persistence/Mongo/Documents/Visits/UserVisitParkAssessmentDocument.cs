using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class UserVisitParkAssessmentDocument
{
    [BsonElement("valueHalfSteps")]
    public byte ValueHalfSteps { get; set; }

    [BsonElement("privateComment")]
    [BsonIgnoreIfNull]
    public string? PrivateComment { get; set; }

    [BsonElement("revision")]
    public int Revision { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
