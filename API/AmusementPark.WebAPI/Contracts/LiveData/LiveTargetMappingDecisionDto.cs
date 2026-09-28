using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.LiveData;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LiveTargetMappingDecisionDto
{
    Verify = 1,
    Correct = 2,
    Suspend = 3,
    Supersede = 4,
    Reject = 5,
}
