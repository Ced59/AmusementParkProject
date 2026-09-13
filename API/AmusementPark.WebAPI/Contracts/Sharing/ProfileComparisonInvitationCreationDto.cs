namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class ProfileComparisonInvitationCreationDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public List<string> Categories { get; set; } = new();
}
