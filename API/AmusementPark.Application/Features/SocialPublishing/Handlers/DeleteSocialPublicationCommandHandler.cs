using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Handlers;

public sealed class DeleteSocialPublicationCommandHandler
    : ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>>
{
    private readonly ISocialPublicationService service;

    public DeleteSocialPublicationCommandHandler(ISocialPublicationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<SocialPublication>> HandleAsync(DeleteSocialPublicationCommand command, CancellationToken cancellationToken = default)
    {
        return this.service.DeleteAsync(command.PublicationId, command.RequestedByUserId, cancellationToken);
    }
}

