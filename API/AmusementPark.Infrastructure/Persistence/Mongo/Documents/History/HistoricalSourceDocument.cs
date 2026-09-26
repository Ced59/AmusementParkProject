using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

[BsonIgnoreExtraElements]
public sealed class HistoricalSourceDocument : MongoDocumentBase
{
    [BsonElement("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [BsonElement("revision")]
    public int Revision { get; set; }

    [BsonElement("revisionOrigin")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalRevisionOrigin RevisionOrigin { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalSourceType Type { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("publisherOrAuthor")]
    public string PublisherOrAuthor { get; set; } = string.Empty;

    [BsonElement("url")]
    [BsonIgnoreIfNull]
    public string? Url { get; set; }

    [BsonElement("bibliographicReference")]
    [BsonIgnoreIfNull]
    public string? BibliographicReference { get; set; }

    [BsonElement("publishedOn")]
    [BsonIgnoreIfNull]
    public string? PublishedOn { get; set; }

    [BsonElement("accessedOn")]
    public string AccessedOn { get; set; } = string.Empty;

    [BsonElement("languageCode")]
    [BsonIgnoreIfNull]
    public string? LanguageCode { get; set; }

    [BsonElement("archiveUrl")]
    [BsonIgnoreIfNull]
    public string? ArchiveUrl { get; set; }

    [BsonElement("scopes")]
    [BsonRepresentation(BsonType.String)]
    public List<HistoricalSourceScope> Scopes { get; set; } = new List<HistoricalSourceScope>();

    [BsonElement("adminNote")]
    [BsonIgnoreIfNull]
    public string? AdminNote { get; set; }

    [BsonElement("accessibility")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalSourceAccessibility Accessibility { get; set; }

    [BsonElement("workflowState")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalEditorialWorkflowState WorkflowState { get; set; }

    [BsonElement("publicationState")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalPublicationState PublicationState { get; set; }

    [BsonElement("transitionReviewEvent")]
    public HistoricalReviewEventDocument TransitionReviewEvent { get; set; } =
        new HistoricalReviewEventDocument();
}
