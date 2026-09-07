namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record VisitRecapShareCandidatesResult(
    IReadOnlyCollection<VisitRecapShareItemResult> Items,
    int TotalEligibleItemCount,
    bool IsTruncated);
