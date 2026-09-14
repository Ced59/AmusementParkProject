using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Application.Features.ParkFit.Services;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

public sealed class CreateParkFitGroupProfileCommandHandler
    : ICommandHandler<CreateParkFitGroupProfileCommand,
        ApplicationResult<ParkFitGroupProfileResult>>
{
    private readonly ParkFitGroupProfileLifecycleService service;

    public CreateParkFitGroupProfileCommandHandler(ParkFitGroupProfileLifecycleService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<ParkFitGroupProfileResult>> HandleAsync(
        CreateParkFitGroupProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.CreateAsync(command.OwnerUserId, command.Profile, cancellationToken);
    }
}
