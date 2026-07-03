using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Commands;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;

public sealed class PreviewBulkParkGraphUpsertCommandHandler : ICommandHandler<PreviewBulkParkGraphUpsertCommand, ApplicationResult<BulkParkGraphUpsertResult>>
{
    private readonly BulkParkGraphUpsertProcessor processor;

    public PreviewBulkParkGraphUpsertCommandHandler(BulkParkGraphUpsertProcessor processor)
    {
        this.processor = processor;
    }

    public Task<ApplicationResult<BulkParkGraphUpsertResult>> HandleAsync(PreviewBulkParkGraphUpsertCommand command, CancellationToken cancellationToken)
    {
        return this.processor.ProcessAsync(command.Request, command.RequestedByUserId, apply: false, cancellationToken);
    }
}

public sealed class ApplyBulkParkGraphUpsertCommandHandler : ICommandHandler<ApplyBulkParkGraphUpsertCommand, ApplicationResult<BulkParkGraphUpsertResult>>
{
    private readonly BulkParkGraphUpsertProcessor processor;

    public ApplyBulkParkGraphUpsertCommandHandler(BulkParkGraphUpsertProcessor processor)
    {
        this.processor = processor;
    }

    public Task<ApplicationResult<BulkParkGraphUpsertResult>> HandleAsync(ApplyBulkParkGraphUpsertCommand command, CancellationToken cancellationToken)
    {
        return this.processor.ProcessAsync(command.Request, command.RequestedByUserId, apply: true, cancellationToken);
    }
}
