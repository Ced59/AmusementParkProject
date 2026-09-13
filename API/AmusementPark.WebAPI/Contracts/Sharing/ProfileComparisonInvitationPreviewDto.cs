namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class ProfileComparisonInvitationPreviewDto
{
    public string Status { get; set; } = string.Empty;

    public string? CreatorDisplayName { get; set; }

    public string? InviteeDisplayName { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? AcceptedAtUtc { get; set; }

    public List<string> Categories { get; set; } = new();

    public bool CanAccept { get; set; }
}
