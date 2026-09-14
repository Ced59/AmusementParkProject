using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Services;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

public sealed class DeleteParkFitGroupProfileCommandHandler
    : ICommandHandler<DeleteParkFitGroupProfileCommand, ApplicationResult>
{
    private readonly ParkFitGroupProfileLifecycleService service;

    public DeleteParkFitGroupProfileCommandHandler(ParkFitGroupProfileLifecycleService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult> HandleAsync(
        DeleteParkFitGroupProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.DeleteAsync(
            command.OwnerUserId,
            command.ProfileId,
            command.ExpectedVersion,
            cancellationToken);
    }
}
