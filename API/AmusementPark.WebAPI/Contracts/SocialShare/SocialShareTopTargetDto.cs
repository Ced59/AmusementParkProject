namespace AmusementPark.WebAPI.Contracts.SocialShare;

public sealed class SocialShareTopTargetDto
{
    public string TargetType { get; set; } = string.Empty;

    public string? TargetId { get; set; }

    public string? TargetTitle { get; set; }

    public string Url { get; set; } = string.Empty;

    public long Count { get; set; }
}
