namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record VisitRecapShareCandidatesResult(
    IReadOnlyCollection<VisitRecapShareItemResult> Items,
    int TotalEligibleItemCount,
    bool IsTruncated,
    IReadOnlyCollection<string>? SavedSelectedParkItemIds = null,
    string? SavedPublicCaption = null,
    bool HasSavedSnapshot = false);
