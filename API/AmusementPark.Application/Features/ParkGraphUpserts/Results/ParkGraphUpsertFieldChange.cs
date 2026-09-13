namespace AmusementPark.Application.Features.ParkGraphUpserts.Results;

public sealed class ParkGraphUpsertFieldChange
{
    public string Field { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }
}
