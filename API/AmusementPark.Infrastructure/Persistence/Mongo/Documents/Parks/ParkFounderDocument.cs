using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;

/// <summary>
/// Document Mongo d'un fondateur de parc.
/// </summary>
public sealed class ParkFounderDocument : MongoDocumentBase
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("occupation")]
    [BsonIgnoreIfNull]
    public string? Occupation { get; set; }

    [BsonElement("birthDate")]
    [BsonIgnoreIfNull]
    public string? BirthDate { get; set; }

    [BsonElement("deathDate")]
    [BsonIgnoreIfNull]
    public string? DeathDate { get; set; }

    [BsonElement("birthPlace")]
    [BsonIgnoreIfNull]
    public string? BirthPlace { get; set; }

    [BsonElement("nationalityCountryCode")]
    [BsonIgnoreIfNull]
    public string? NationalityCountryCode { get; set; }

    [BsonElement("websiteUrl")]
    [BsonIgnoreIfNull]
    public string? WebsiteUrl { get; set; }

    [BsonElement("biography")]
    public List<LocalizedTextDocument> Biography { get; set; } = new();
}
