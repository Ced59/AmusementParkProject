using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;

namespace AmusementPark.Application.Tests.Features.BackgroundJobs.Services;

internal sealed class StubHandler : IDurableBackgroundJobHandler
{
    private readonly Func<
        DurableBackgroundJobExecutionContext,
        CancellationToken,
        Task<DurableBackgroundJobHandlerResult>> execute;

    public StubHandler(
        DurableBackgroundJobHandlerDefinition definition,
        Func<
            DurableBackgroundJobExecutionContext,
            CancellationToken,
            Task<DurableBackgroundJobHandlerResult>> execute)
    {
        this.Definition = definition;
        this.execute = execute;
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; }

    public Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        return this.execute(context, cancellationToken);
    }
}
