using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Commands;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;

public sealed class PreviewParkGraphUpsertCommandHandler : ICommandHandler<PreviewParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>>
{
    private readonly ParkGraphUpsertProcessor processor;

    public PreviewParkGraphUpsertCommandHandler(ParkGraphUpsertProcessor processor)
    {
        this.processor = processor;
    }

    public Task<ApplicationResult<ParkGraphUpsertResult>> HandleAsync(PreviewParkGraphUpsertCommand command, CancellationToken cancellationToken)
    {
        return this.processor.PreviewAsync(command.Request, command.RequestedByUserId, cancellationToken);
    }
}
