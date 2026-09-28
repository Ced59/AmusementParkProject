namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class LiveExternalTargetDto
{
    public LiveTargetTypeDto Type { get; init; }

    public string Id { get; init; } = string.Empty;

    public string? ParentId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string? ParentDisplayName { get; init; }

    public string CountryCode { get; init; } = string.Empty;
}
