namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed record PublicLiveSourceDto(
    string Id,
    string DisplayName,
    string Type,
    string AttributionText,
    string AttributionUrl);
