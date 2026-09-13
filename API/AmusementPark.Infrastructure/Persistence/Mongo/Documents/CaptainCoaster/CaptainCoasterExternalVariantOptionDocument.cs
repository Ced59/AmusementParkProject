using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;

public sealed class CaptainCoasterExternalVariantOptionDocument
{
    [BsonElement("externalVariantId")]
    public string ExternalVariantId { get; set; } = string.Empty;

    [BsonElement("displayLabel")]
    public string DisplayLabel { get; set; } = string.Empty;

    [BsonElement("candidateLocalEntityId")]
    [BsonIgnoreIfNull]
    public string? CandidateLocalEntityId { get; set; }

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

    [BsonElement("isSuggested")]
    public bool IsSuggested { get; set; }

    [BsonElement("changes")]
    public List<CaptainCoasterFieldChangeDocument> Changes { get; set; } = new List<CaptainCoasterFieldChangeDocument>();
}
