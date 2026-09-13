using AmusementPark.Application.Features.BackgroundJobs.Models;

namespace AmusementPark.Application.Features.BackgroundJobs.Ports;

public interface IDurableBackgroundJobHandlerResolver
{
    IReadOnlyCollection<DurableBackgroundJobHandlerDefinition> Definitions { get; }

    bool TryResolve(string kind, out IDurableBackgroundJobHandler? handler);
}
