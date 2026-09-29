namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record PublicParkLiveItemsResult(
    string ParkId,
    string ParkDisplayName,
    DateTime AsOfUtc,
    IReadOnlyCollection<PublicLiveTargetResult> Items);
