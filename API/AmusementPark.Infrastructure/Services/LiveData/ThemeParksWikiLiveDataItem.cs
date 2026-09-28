using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmusementPark.Infrastructure.Services.LiveData;

internal sealed class ThemeParksWikiLiveDataItem
{
    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("name")]
    public JsonElement? Name { get; set; }

    [JsonPropertyName("entityType")]
    public JsonElement? EntityType { get; set; }

    [JsonPropertyName("status")]
    public JsonElement? Status { get; set; }

    [JsonPropertyName("lastUpdated")]
    public JsonElement? LastUpdated { get; set; }

    [JsonPropertyName("queue")]
    public JsonElement? Queue { get; set; }
}
