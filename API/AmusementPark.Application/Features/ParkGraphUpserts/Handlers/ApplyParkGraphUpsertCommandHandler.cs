using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Commands;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;

public sealed class ApplyParkGraphUpsertCommandHandler : ICommandHandler<ApplyParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>>
{
    private readonly ParkGraphUpsertProcessor processor;

    public ApplyParkGraphUpsertCommandHandler(ParkGraphUpsertProcessor processor)
    {
        this.processor = processor;
    }

    public Task<ApplicationResult<ParkGraphUpsertResult>> HandleAsync(ApplyParkGraphUpsertCommand command, CancellationToken cancellationToken)
    {
        return this.processor.ApplyAsync(command.Request, command.RequestedByUserId, cancellationToken);
    }
}
