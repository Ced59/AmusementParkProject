using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

public sealed class PassportProfileShareScopeRegistrationDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("scopeKey")]
    public string ScopeKey { get; set; } = string.Empty;

    [BsonElement("ownerUserId")]
    public string OwnerUserId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("selectedYears")]
    public List<int> SelectedYears { get; set; } = new();
}
