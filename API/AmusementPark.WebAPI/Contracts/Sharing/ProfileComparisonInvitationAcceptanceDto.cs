namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class ProfileComparisonInvitationAcceptanceDto
{
    public string ComparisonId { get; set; } = string.Empty;

    public DateTime AcceptedAtUtc { get; set; }

    public List<string> Categories { get; set; } = new();
}
