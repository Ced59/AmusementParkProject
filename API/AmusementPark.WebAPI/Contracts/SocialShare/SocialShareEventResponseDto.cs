namespace AmusementPark.WebAPI.Contracts.SocialShare;

public sealed class SocialShareEventResponseDto
{
    public bool Accepted { get; set; }

    public DateTime? OccurredAtUtc { get; set; }
}
