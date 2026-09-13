using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ParkGraphUpserts;

public sealed class ParkGraphUpsertFieldChangeDto
{
    public string Field { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }
}
