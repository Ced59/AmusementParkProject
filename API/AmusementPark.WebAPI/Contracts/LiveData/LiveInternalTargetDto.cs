namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class LiveInternalTargetDto
{
    public LiveTargetTypeDto Type { get; init; }

    public string Id { get; init; } = string.Empty;

    public string ParkId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string ParkDisplayName { get; init; } = string.Empty;

    public string CountryCode { get; init; } = string.Empty;
}
