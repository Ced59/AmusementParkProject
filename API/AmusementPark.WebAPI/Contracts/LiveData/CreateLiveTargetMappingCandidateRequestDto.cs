namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class CreateLiveTargetMappingCandidateRequestDto
{
    public string SourceId { get; init; } = string.Empty;

    public LiveTargetTypeDto TargetType { get; init; }

    public string ExternalTargetId { get; init; } = string.Empty;

    public string? ExternalParentTargetId { get; init; }

    public string ExternalDisplayName { get; init; } = string.Empty;

    public string? ExternalParentDisplayName { get; init; }

    public string ExternalCountryCode { get; init; } = string.Empty;

    public string? SuggestedInternalTargetId { get; init; }

    public string? SuggestedParkId { get; init; }
}
