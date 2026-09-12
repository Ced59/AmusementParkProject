using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Handlers;

public sealed class RefreshParkAnnouncementPreviewCommandHandler
    : ICommandHandler<RefreshParkAnnouncementPreviewCommand, ApplicationResult<SocialPublication>>
{
    private readonly ISocialPublicationService service;

    public RefreshParkAnnouncementPreviewCommandHandler(ISocialPublicationService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<SocialPublication>> HandleAsync(
        RefreshParkAnnouncementPreviewCommand command,
        CancellationToken cancellationToken = default)
    {
        return this.service.RefreshParkAnnouncementPreviewAsync(
            command.ParkId,
            command.RequestedByUserId,
            cancellationToken);
    }
}

