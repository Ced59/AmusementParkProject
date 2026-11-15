using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Application.Features.SocialPublishing;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Handlers;

public sealed class DeleteSocialPublicationCommandHandler
    : ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>>
{
    private readonly ISocialPublicationService service;
    private readonly ISocialPublicationRepository repository;

    public DeleteSocialPublicationCommandHandler(ISocialPublicationService service, ISocialPublicationRepository repository)
    {
        this.service = service;
        this.repository = repository;
    }

    public async Task<ApplicationResult<SocialPublication>> HandleAsync(DeleteSocialPublicationCommand command, CancellationToken cancellationToken = default)
    {
        if (command.RequireRequesterOwnership)
        {
            SocialPublication? publication = await this.repository.GetByIdAsync(command.PublicationId, cancellationToken);
            if (publication is null
                || string.IsNullOrWhiteSpace(command.RequestedByUserId)
                || publication.RequestedByUserId != command.RequestedByUserId
                || publication.Network != SocialNetwork.Facebook
                || publication.Trigger != SocialPublicationTrigger.Manual)
            {
                return ApplicationResult<SocialPublication>.Failure(
                    SocialPublishingApplicationErrors.PublicationNotFound(command.PublicationId));
            }
        }

        return await this.service.DeleteAsync(command.PublicationId, command.RequestedByUserId, cancellationToken);
    }
}

