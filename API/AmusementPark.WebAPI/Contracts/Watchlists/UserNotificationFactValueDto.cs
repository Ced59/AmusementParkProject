namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class UserNotificationFactValueDto
{
    public string Kind { get; init; } = string.Empty;

    public string CanonicalValue { get; init; } = string.Empty;

    public string? UnitCode { get; init; }
}
