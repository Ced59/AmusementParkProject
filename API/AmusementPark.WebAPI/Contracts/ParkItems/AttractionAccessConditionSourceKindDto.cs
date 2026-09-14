using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttractionAccessConditionSourceKindDto
{
    Unknown,
    Official,
    OperatorProvided,
    VerifiedSecondary,
    CommunityUnverified,
}
