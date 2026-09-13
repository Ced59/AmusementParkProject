using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class GlobalRatingSuggestionPreferenceDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; } = true;
}
