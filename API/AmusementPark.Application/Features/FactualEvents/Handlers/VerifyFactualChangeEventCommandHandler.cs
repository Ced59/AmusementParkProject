using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Commands;
using AmusementPark.Application.Features.FactualEvents.Services;

namespace AmusementPark.Application.Features.FactualEvents.Handlers;

public sealed class VerifyFactualChangeEventCommandHandler
    : ICommandHandler<VerifyFactualChangeEventCommand, ApplicationResult>
{
    private readonly FactualChangeEventAdministrationService service;

    public VerifyFactualChangeEventCommandHandler(FactualChangeEventAdministrationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult> HandleAsync(
        VerifyFactualChangeEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.VerifyAsync(
            command.EventId,
            command.ExpectedVersion,
            cancellationToken);
    }
}
