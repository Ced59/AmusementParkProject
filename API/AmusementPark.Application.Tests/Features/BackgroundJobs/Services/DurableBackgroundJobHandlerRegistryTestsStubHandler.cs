using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.BackgroundJobs.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.BackgroundJobs.Services;

internal sealed class DurableBackgroundJobHandlerRegistryTestsStubHandler : IDurableBackgroundJobHandler
{
    public DurableBackgroundJobHandlerRegistryTestsStubHandler(DurableBackgroundJobHandlerDefinition definition)
    {
        this.Definition = definition;
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; }

    public Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(DurableBackgroundJobHandlerResult.Success());
    }
}
