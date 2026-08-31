using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;

namespace AmusementPark.Application.Features.BackgroundJobs.Services;

public sealed class DurableBackgroundJobHandlerRegistry : IDurableBackgroundJobHandlerResolver
{
    private readonly IReadOnlyDictionary<string, IDurableBackgroundJobHandler> handlers;

    public DurableBackgroundJobHandlerRegistry(IEnumerable<IDurableBackgroundJobHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        Dictionary<string, IDurableBackgroundJobHandler> handlersByKind =
            new Dictionary<string, IDurableBackgroundJobHandler>(StringComparer.Ordinal);
        foreach (IDurableBackgroundJobHandler handler in handlers)
        {
            ArgumentNullException.ThrowIfNull(handler);
            DurableBackgroundJobHandlerDefinition definition = handler.Definition
                ?? throw new InvalidOperationException("A durable background job handler returned no definition.");
            if (!handlersByKind.TryAdd(definition.Kind, handler))
            {
                throw new InvalidOperationException(
                    $"Several durable background job handlers are registered for kind '{definition.Kind}'.");
            }
        }

        this.handlers = handlersByKind;
        this.Definitions = handlersByKind.Values
            .Select(static handler => handler.Definition)
            .OrderBy(static definition => definition.Kind, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyCollection<DurableBackgroundJobHandlerDefinition> Definitions { get; }

    public bool TryResolve(string kind, out IDurableBackgroundJobHandler? handler)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            handler = null;
            return false;
        }

        return this.handlers.TryGetValue(kind.Trim(), out handler);
    }
}
