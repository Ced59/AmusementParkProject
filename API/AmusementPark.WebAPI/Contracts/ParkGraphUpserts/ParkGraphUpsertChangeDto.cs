using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ParkGraphUpserts;

public sealed class ParkGraphUpsertChangeDto
{
    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? EntityKey { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string ChangeType { get; set; } = string.Empty;

    public string MatchedBy { get; set; } = string.Empty;

    public List<ParkGraphUpsertFieldChangeDto> Fields { get; set; } = new List<ParkGraphUpsertFieldChangeDto>();
}
