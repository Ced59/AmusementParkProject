using AmusementPark.WebAPI.Contracts.Common;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.SocialPublishing;

public sealed class SocialPublicationDto
{
    public string Id { get; set; } = string.Empty;

    public string Network { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Trigger { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? SourceEntityType { get; set; }

    public string? SourceEntityId { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public DateTime? AttemptedAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime? LastSynchronizedAtUtc { get; set; }

    public string? ExternalPostId { get; set; }

    public string? ExternalPostUrl { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }
}
