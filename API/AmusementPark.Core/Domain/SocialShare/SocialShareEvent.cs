using AmusementPark.Core.Abstractions;

namespace AmusementPark.Core.Domain.SocialShare;

public sealed class SocialShareEvent : AuditableEntity
{
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public SocialShareTargetType TargetType { get; set; } = SocialShareTargetType.Page;

    public string? TargetId { get; set; }

    public string? TargetTitle { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? LanguageCode { get; set; }

    public SocialShareChannel Channel { get; set; } = SocialShareChannel.Copy;

    public SocialShareVisitorKind VisitorKind { get; set; } = SocialShareVisitorKind.Anonymous;

    public string? UserId { get; set; }
}
