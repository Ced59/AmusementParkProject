using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmusementPark.Infrastructure.Services.LiveData;

internal sealed class ThemeParksWikiLiveDataItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("entityType")]
    public string? EntityType { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("queue")]
    public JsonElement? Queue { get; set; }
}
