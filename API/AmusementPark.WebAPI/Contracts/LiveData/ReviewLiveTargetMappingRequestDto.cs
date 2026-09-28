namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class ReviewLiveTargetMappingRequestDto
{
    public int ExpectedRevision { get; init; }

    public LiveTargetMappingDecisionDto Decision { get; init; }

    public string? InternalTargetId { get; init; }

    public string? ParkId { get; init; }

    public string? ReviewNote { get; init; }
}
