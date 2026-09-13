namespace AmusementPark.WebAPI.Contracts.SocialShare;

public sealed class SocialShareEventRequestDto
{
    public string? TargetType { get; set; }

    public string? TargetId { get; set; }

    public string? TargetTitle { get; set; }

    public string? Url { get; set; }

    public string? LanguageCode { get; set; }

    public string? Channel { get; set; }
}
