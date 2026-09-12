using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Contracts;

public sealed record SocialPublisherDescriptor(
    SocialNetwork Network,
    string DisplayName,
    bool IsEnabled,
    bool IsConfigured,
    string? TargetUrl,
    bool SupportsAutomaticParkAnnouncements);

