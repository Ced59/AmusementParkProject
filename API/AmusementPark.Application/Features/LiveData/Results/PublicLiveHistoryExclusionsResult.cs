namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record PublicLiveHistoryExclusionsResult(
    int DuplicateObservations,
    int OutsideActiveWindow,
    int NonOperatingStatus,
    int MissingStandbyWait,
    int Total);
