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

public sealed record SocialPublishingOverview(
    IReadOnlyCollection<SocialPublisherDescriptor> Publishers,
    IReadOnlyCollection<SocialPublication> RecentPublications);
