using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Videos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VideoHostingProviderDto
{
    OTHER = 0,
    YOUTUBE = 1,
    DAILYMOTION = 2,
    VIMEO = 3,
}
