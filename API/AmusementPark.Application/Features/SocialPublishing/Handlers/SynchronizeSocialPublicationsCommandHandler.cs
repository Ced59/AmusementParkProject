using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Handlers;

public sealed class SynchronizeSocialPublicationsCommandHandler
    : ICommandHandler<SynchronizeSocialPublicationsCommand, SocialPublicationSynchronizationResult>
{
    private readonly ISocialPublicationService service;

    public SynchronizeSocialPublicationsCommandHandler(ISocialPublicationService service)
    {
        this.service = service;
    }

    public Task<SocialPublicationSynchronizationResult> HandleAsync(SynchronizeSocialPublicationsCommand command, CancellationToken cancellationToken = default)
    {
        return this.service.SynchronizeAsync(command.Limit, cancellationToken);
    }
}

