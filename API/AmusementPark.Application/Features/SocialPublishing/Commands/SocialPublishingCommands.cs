using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Commands;

public sealed record PublishSocialLinkCommand(
    SocialLinkPublicationRequest Request,
    string? RequestedByUserId) : ICommand<ApplicationResult<SocialPublication>>;

public sealed record RetrySocialPublicationCommand(
    string PublicationId,
    string? RequestedByUserId) : ICommand<ApplicationResult<SocialPublication>>;

public sealed record UpdateSocialPublicationCommand(
    string PublicationId,
    string? Message,
    string? RequestedByUserId) : ICommand<ApplicationResult<SocialPublication>>;

public sealed record DeleteSocialPublicationCommand(
    string PublicationId,
    string? RequestedByUserId) : ICommand<ApplicationResult<SocialPublication>>;

public sealed record SynchronizeSocialPublicationsCommand(
    int Limit) : ICommand<SocialPublicationSynchronizationResult>;

public sealed record RefreshParkAnnouncementPreviewCommand(
    string ParkId,
    string? RequestedByUserId) : ICommand<ApplicationResult<SocialPublication>>;
