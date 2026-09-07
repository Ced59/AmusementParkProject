namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class VisitRecapShareCandidatesDto
{
    public List<VisitRecapShareItemDto> Items { get; set; } = new();

    public int TotalEligibleItemCount { get; set; }

    public bool IsTruncated { get; set; }
}
