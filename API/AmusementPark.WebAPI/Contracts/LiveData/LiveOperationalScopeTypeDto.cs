using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.LiveData;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LiveOperationalScopeTypeDto
{
    Source = 0,
    Park = 1,
    Target = 2,
}
