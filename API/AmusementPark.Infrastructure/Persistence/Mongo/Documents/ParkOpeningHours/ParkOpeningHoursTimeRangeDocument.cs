using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;

[BsonIgnoreExtraElements]
public sealed class ParkOpeningHoursTimeRangeDocument
{
    [BsonElement("opensAt")]
    public string OpensAt { get; set; } = string.Empty;

    [BsonElement("closesAt")]
    public string ClosesAt { get; set; } = string.Empty;

    [BsonElement("closesNextDay")]
    public bool ClosesNextDay { get; set; }

    [BsonElement("lastAdmissionAt")]
    [BsonIgnoreIfNull]
    public string? LastAdmissionAt { get; set; }

    [BsonElement("lastAdmissionNextDay")]
    public bool LastAdmissionNextDay { get; set; }
}
