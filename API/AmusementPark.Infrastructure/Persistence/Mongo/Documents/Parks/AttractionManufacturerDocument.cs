using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;

/// <summary>
/// Document Mongo d'un constructeur d'attractions.
/// </summary>
public sealed class AttractionManufacturerDocument : MongoDocumentBase
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("legalName")]
    [BsonIgnoreIfNull]
    public string? LegalName { get; set; }

    [BsonElement("foundedYear")]
    [BsonIgnoreIfNull]
    public int? FoundedYear { get; set; }

    [BsonElement("closedYear")]
    [BsonIgnoreIfNull]
    public int? ClosedYear { get; set; }

    [BsonElement("contactDetails")]
    [BsonIgnoreIfNull]
    public ParkReferenceContactDetailsDocument? ContactDetails { get; set; }

    [BsonElement("biography")]
    public List<LocalizedTextDocument> Biography { get; set; } = new();

    [BsonElement("currentLogoImageId")]
    [BsonIgnoreIfNull]
    public string? CurrentLogoImageId { get; set; }

    [BsonElement("isVisible")]
    public bool IsVisible { get; set; } = true;

    [BsonElement("adminReviewStatus")]
    [BsonRepresentation(BsonType.String)]
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;

    [BsonElement("adminReviewPriority")]
    public int AdminReviewPriority { get; set; }
}
