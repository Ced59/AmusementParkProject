namespace AmusementPark.WebAPI.Contracts.FactualEvents;

public sealed class FactualChangeTargetAdminDto
{
    public string Type { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string? ParentParkName { get; init; }
}
