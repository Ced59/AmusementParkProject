using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

[BsonIgnoreExtraElements]
public sealed class HistoricalFactDocument : MongoDocumentBase
{
    [BsonElement("factId")]
    public string FactId { get; set; } = string.Empty;

    [BsonElement("revision")]
    public int Revision { get; set; }

    [BsonElement("revisionOrigin")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalRevisionOrigin RevisionOrigin { get; set; }

    [BsonElement("supersedesRevision")]
    [BsonIgnoreIfNull]
    public int? SupersedesRevision { get; set; }

    [BsonElement("subject")]
    public HistoricalSubjectDocument Subject { get; set; } = new HistoricalSubjectDocument();

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalFactType Type { get; set; }

    [BsonElement("period")]
    public HistoricalPeriodDocument Period { get; set; } = new HistoricalPeriodDocument();

    [BsonElement("state")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalFactState State { get; set; }

    [BsonElement("importance")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalImportance Importance { get; set; }

    [BsonElement("workflowState")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalEditorialWorkflowState WorkflowState { get; set; }

    [BsonElement("publicationState")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalPublicationState PublicationState { get; set; }

    [BsonElement("publicUncertaintyExplanation")]
    public List<LocalizedTextDocument> PublicUncertaintyExplanation { get; set; } = new List<LocalizedTextDocument>();

    [BsonElement("lifecycleBoundaryMeaning")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public LifecycleBoundaryMeaning? LifecycleBoundaryMeaning { get; set; }

    [BsonElement("attributeKind")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public HistoricalAttributeKind? AttributeKind { get; set; }

    [BsonElement("attributeBoundaryMeaning")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public AttributeBoundaryMeaning? AttributeBoundaryMeaning { get; set; }

    [BsonElement("sequenceWithinDate")]
    [BsonIgnoreIfNull]
    public int? SequenceWithinDate { get; set; }

    [BsonElement("sources")]
    public List<HistoricalSourceRevisionDocument> Sources { get; set; } =
        new List<HistoricalSourceRevisionDocument>();

    [BsonElement("structuredValue")]
    [BsonIgnoreIfNull]
    public string? StructuredValue { get; set; }

    [BsonElement("otherTypeLabel")]
    [BsonIgnoreIfNull]
    public string? OtherTypeLabel { get; set; }

    [BsonElement("narrativeContentId")]
    [BsonIgnoreIfNull]
    public string? NarrativeContentId { get; set; }

    [BsonElement("verifiedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? VerifiedAtUtc { get; set; }

    [BsonElement("publishedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? PublishedAtUtc { get; set; }

    [BsonElement("publicationMethodologyVersion")]
    [BsonIgnoreIfNull]
    public string? PublicationMethodologyVersion { get; set; }

    [BsonElement("transitionReviewEvent")]
    public HistoricalReviewEventDocument TransitionReviewEvent { get; set; } =
        new HistoricalReviewEventDocument();
}
