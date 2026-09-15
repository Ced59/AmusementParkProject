namespace AmusementPark.WebAPI.Contracts.FactualEvents;

public sealed class FactualFactValueAdminDto
{
    public string Kind { get; init; } = string.Empty;

    public string CanonicalValue { get; init; } = string.Empty;

    public string? UnitCode { get; init; }
}
