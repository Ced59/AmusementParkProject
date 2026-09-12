using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Handlers;

public sealed class PublishSocialLinkCommandHandler
    : ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>>
{
    private readonly ISocialPublicationComposerService service;

    public PublishSocialLinkCommandHandler(ISocialPublicationComposerService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<SocialPublication>> HandleAsync(PublishSocialLinkCommand command, CancellationToken cancellationToken = default)
    {
        return this.service.PublishAsync(command.Request, command.RequestedByUserId, cancellationToken);
    }
}

