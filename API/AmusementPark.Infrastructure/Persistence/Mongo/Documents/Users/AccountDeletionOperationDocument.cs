using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;

[BsonIgnoreExtraElements]
public sealed class AccountDeletionOperationDocument : MongoDocumentBase
{
    [BsonElement("userKey")]
    public string UserKey { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;
}
