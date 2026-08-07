using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Contracts;

public sealed record SocialLinkPublicationRequest(
    SocialNetwork Network,
    string? Message,
    string? Url,
    string? PreviewImageId = null);

public enum SocialPublicationTargetKind
{
    Park = 0,
    ParkItem = 1,
    Video = 2,
    Page = 3,
}

public sealed record SocialPublicationImageOption(
    string Id,
    string Label,
    bool IsCurrent,
    int Width,
    int Height);

public sealed record SocialPublicationDraft(
    string Url,
    string DefaultMessage,
    SocialPublicationTargetKind TargetKind,
    string TargetName,
    ImageOwnerType? ImageOwnerType,
    string? ImageOwnerId,
    PagedResult<SocialPublicationImageOption> Images);

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
