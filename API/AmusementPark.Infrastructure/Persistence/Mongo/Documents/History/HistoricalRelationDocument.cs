using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

[BsonIgnoreExtraElements]
public sealed class HistoricalRelationDocument : MongoDocumentBase
{
    [BsonElement("relationId")]
    public string RelationId { get; set; } = string.Empty;

    [BsonElement("revision")]
    public int Revision { get; set; }

    [BsonElement("revisionOrigin")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalRevisionOrigin RevisionOrigin { get; set; }

    [BsonElement("supersedesRevision")]
    [BsonIgnoreIfNull]
    public int? SupersedesRevision { get; set; }

    [BsonElement("source")]
    public HistoricalSubjectDocument Source { get; set; } = new HistoricalSubjectDocument();

    [BsonElement("target")]
    public HistoricalSubjectDocument Target { get; set; } = new HistoricalSubjectDocument();

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalRelationType Type { get; set; }

    [BsonElement("direction")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalRelationDirection Direction { get; set; }

    [BsonElement("period")]
    public HistoricalPeriodDocument Period { get; set; } = new HistoricalPeriodDocument();

    [BsonElement("state")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalFactState State { get; set; }

    [BsonElement("workflowState")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalEditorialWorkflowState WorkflowState { get; set; }

    [BsonElement("publicationState")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalPublicationState PublicationState { get; set; }

    [BsonElement("publicUncertaintyExplanation")]
    public List<LocalizedTextDocument> PublicUncertaintyExplanation { get; set; } = new List<LocalizedTextDocument>();

    [BsonElement("sources")]
    public List<HistoricalRelationSourceRevisionDocument> Sources { get; set; } =
        new List<HistoricalRelationSourceRevisionDocument>();

    [BsonElement("editorialNote")]
    [BsonIgnoreIfNull]
    public string? EditorialNote { get; set; }

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
    public HistoricalReviewEventDocument TransitionReviewEvent { get; set; } = new HistoricalReviewEventDocument();
}
