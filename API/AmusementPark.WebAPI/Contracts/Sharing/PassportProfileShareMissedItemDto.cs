using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PassportProfileShareMissedItemDto
{
    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? OccurrenceCount { get; set; }
}
