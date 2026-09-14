using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.ParkFit;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ParkFitUnknownDataPolicyDto
{
    KeepWithWarning,
    ExcludeUnknown,
    KnownOnly,
}
