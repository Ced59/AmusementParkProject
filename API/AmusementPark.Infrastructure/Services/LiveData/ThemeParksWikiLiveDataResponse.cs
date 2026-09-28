using System.Text.Json.Serialization;

namespace AmusementPark.Infrastructure.Services.LiveData;

internal sealed class ThemeParksWikiLiveDataResponse
{
    [JsonPropertyName("liveData")]
    public List<ThemeParksWikiLiveDataItem>? LiveData { get; set; }
}
