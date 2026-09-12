using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Handlers;

public sealed class RetryParkAnnouncementPublicationCommandHandler
    : ICommandHandler<RetryParkAnnouncementPublicationCommand, ApplicationResult<SocialPublication>>
{
    private readonly ISocialPublicationService service;

    public RetryParkAnnouncementPublicationCommandHandler(ISocialPublicationService service)
    {
        this.service = service;
    }

    public async Task<ApplicationResult<SocialPublication>> HandleAsync(
        RetryParkAnnouncementPublicationCommand command,
        CancellationToken cancellationToken = default)
    {
        return await this.service.RetryParkAnnouncementAsync(
            command.ParkId,
            command.PublicationId,
            command.RequestedByUserId,
            cancellationToken);
    }
}

