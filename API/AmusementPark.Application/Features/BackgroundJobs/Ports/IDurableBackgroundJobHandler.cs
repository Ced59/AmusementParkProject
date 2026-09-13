using AmusementPark.Application.Features.BackgroundJobs.Models;

namespace AmusementPark.Application.Features.BackgroundJobs.Ports;

public interface IDurableBackgroundJobHandler
{
    DurableBackgroundJobHandlerDefinition Definition { get; }

    Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken);
}
