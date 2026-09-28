using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.LiveData;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LiveMappingStatusDto
{
    Candidate = 1,
    Verified = 2,
    Suspended = 3,
    Superseded = 4,
    Rejected = 5,
}
