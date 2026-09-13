using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Seo;

public sealed class SeoIndexNowSubmissionDocument
{
    [BsonElement("wasRequested")]
    public bool WasRequested { get; set; }

    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; }

    [BsonElement("isSuccess")]
    public bool IsSuccess { get; set; }

    [BsonElement("submittedUrlCount")]
    public int SubmittedUrlCount { get; set; }

    [BsonElement("acceptedEndpoints")]
    public List<string> AcceptedEndpoints { get; set; } = new List<string>();

    [BsonElement("errors")]
    public List<string> Errors { get; set; } = new List<string>();
}
