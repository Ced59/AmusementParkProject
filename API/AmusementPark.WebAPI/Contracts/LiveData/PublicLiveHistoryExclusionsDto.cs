namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed record PublicLiveHistoryExclusionsDto(
    int DuplicateObservations,
    int OutsideActiveWindow,
    int NonOperatingStatus,
    int MissingStandbyWait,
    int Total);
