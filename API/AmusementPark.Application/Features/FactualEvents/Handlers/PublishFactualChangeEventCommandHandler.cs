using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Commands;
using AmusementPark.Application.Features.FactualEvents.Services;

namespace AmusementPark.Application.Features.FactualEvents.Handlers;

public sealed class PublishFactualChangeEventCommandHandler
    : ICommandHandler<PublishFactualChangeEventCommand, ApplicationResult>
{
    private readonly FactualChangeEventAdministrationService service;

    public PublishFactualChangeEventCommandHandler(FactualChangeEventAdministrationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult> HandleAsync(
        PublishFactualChangeEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.PublishAsync(
            command.EventId,
            command.ExpectedVersion,
            cancellationToken);
    }
}
