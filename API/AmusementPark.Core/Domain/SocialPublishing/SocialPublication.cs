using AmusementPark.Core.Abstractions;

namespace AmusementPark.Core.Domain.SocialPublishing;

public sealed class SocialPublication : AuditableEntity
{
    public SocialNetwork Network { get; set; } = SocialNetwork.Facebook;

    public SocialPublicationStatus Status { get; set; } = SocialPublicationStatus.Pending;

    public SocialPublicationTrigger Trigger { get; set; } = SocialPublicationTrigger.Manual;

    public string Message { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? SourceEntityType { get; set; }

    public string? SourceEntityId { get; set; }

    public string? RequestedByUserId { get; set; }

    public string? DeduplicationKey { get; set; }

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? AttemptedAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime? LastSynchronizedAtUtc { get; set; }

    public string? ExternalPostId { get; set; }

    public string? ExternalPostUrl { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }
}
