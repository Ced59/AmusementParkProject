using AmusementPark.Core.Domain.Sharing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class PassportProfileShareSelectionDocument
{
    [BsonElement("selectedYears")]
    public List<int> SelectedYears { get; set; } = new();

    [BsonElement("selectedParkIds")]
    public List<string> SelectedParkIds { get; set; } = new();

    [BsonElement("selectedRatingKeys")]
    public List<string> SelectedRatingKeys { get; set; } = new();

    [BsonElement("publicCaption")]
    [BsonIgnoreIfNull]
    public string? PublicCaption { get; set; }

    [BsonElement("visibility")]
    [BsonRepresentation(BsonType.String)]
    public ShareVisibility Visibility { get; set; } = ShareVisibility.Unlisted;

    [BsonElement("allowsComparisons")]
    public bool AllowsComparisons { get; set; }
}
