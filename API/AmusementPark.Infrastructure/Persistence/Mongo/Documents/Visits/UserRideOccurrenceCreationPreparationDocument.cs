using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class UserRideOccurrenceCreationPreparationDocument
{
    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("visitDate")]
    public VisitDateDocument VisitDate { get; set; } = new VisitDateDocument();

    [BsonElement("timeZoneId")]
    [BsonIgnoreIfNull]
    public string? TimeZoneId { get; set; }

    [BsonElement("serviceDayConvention")]
    [BsonRepresentation(BsonType.String)]
    public LocalServiceDayConvention ServiceDayConvention { get; set; }

    [BsonElement("items")]
    public List<UserRideOccurrenceCreationPreparationItemDocument> Items { get; set; } =
        new List<UserRideOccurrenceCreationPreparationItemDocument>();
}
