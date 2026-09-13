using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Seo;

public sealed class SeoSitemapSettingsDocument : MongoDocumentBase
{
    [BsonElement("isIndexNowEnabled")]
    public bool IsIndexNowEnabled { get; set; }

    [BsonElement("submitToIndexNowAfterManualGeneration")]
    public bool SubmitToIndexNowAfterManualGeneration { get; set; }

    [BsonElement("submitToIndexNowAfterAutomaticGeneration")]
    public bool SubmitToIndexNowAfterAutomaticGeneration { get; set; }

    [BsonElement("indexNowKey")]
    public string IndexNowKey { get; set; } = string.Empty;

    [BsonElement("indexNowKeyLocation")]
    public string IndexNowKeyLocation { get; set; } = string.Empty;

    [BsonElement("indexNowEndpoints")]
    public List<string> IndexNowEndpoints { get; set; } = new List<string>();
}
