using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Contracts;

public sealed record SocialLinkPublicationRequest(
    SocialNetwork Network,
    string? Message,
    string? Url);

public sealed record SocialPublisherDescriptor(
    SocialNetwork Network,
    string DisplayName,
    bool IsEnabled,
    bool IsConfigured,
    string? TargetUrl,
    bool SupportsAutomaticParkAnnouncements);

public sealed record SocialPublisherRequest(string Message, string Url);

public sealed record SocialPublisherResult(
    bool IsSuccess,
    string? ExternalPostId,
    string? ExternalPostUrl,
    string? FailureCode,
    string? FailureMessage);

public sealed record SocialPublisherOperationResult(
    bool IsSuccess,
    bool IsMissing,
    string? FailureCode,
    string? FailureMessage);

public sealed record SocialPublisherPostSnapshotResult(
    bool IsSuccess,
    bool Exists,
    string? Message,
    string? ExternalPostUrl,
    string? FailureCode,
    string? FailureMessage);

public sealed record SocialPublicationSynchronizationResult(
    int CheckedCount,
    int UpdatedCount,
    int DeletedCount,
    int FailureCount);

public enum SocialWebhookChangeKind
{
    Updated = 0,
    Deleted = 1,
}

public sealed record SocialWebhookChange(
    string ExternalPostId,
    SocialWebhookChangeKind Kind,
    string? Message);

public sealed record SocialPublishingOverview(
    IReadOnlyCollection<SocialPublisherDescriptor> Publishers,
    IReadOnlyCollection<SocialPublication> RecentPublications);
