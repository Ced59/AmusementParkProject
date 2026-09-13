using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ContextualBlocks.Contracts;

public sealed class ContextualBlockExportMetadata
{
    public string Source { get; init; } = "admin-contextual-block-export";

    public DateTime ExportedAtUtc { get; init; }
}
