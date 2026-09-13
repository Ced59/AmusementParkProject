namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class ProfileComparisonSummaryDto
{
    public string ShareId { get; set; } = string.Empty;

    public string? OtherDisplayName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<string> Categories { get; set; } = new();
}
