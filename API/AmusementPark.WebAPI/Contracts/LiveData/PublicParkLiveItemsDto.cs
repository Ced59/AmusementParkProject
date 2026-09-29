namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed record PublicParkLiveItemsDto(
    string ParkId,
    string ParkDisplayName,
    DateTime AsOfUtc,
    IReadOnlyCollection<PublicLiveTargetDto> Items);
