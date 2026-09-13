using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ContextualBlocks.Contracts;

public sealed class ContextualBlockExportTarget
{
    public string EntityType { get; init; } = string.Empty;

    public string EntityId { get; init; } = string.Empty;
}
