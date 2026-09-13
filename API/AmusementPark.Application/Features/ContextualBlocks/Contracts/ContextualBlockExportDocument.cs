using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ContextualBlocks.Contracts;

public sealed class ContextualBlockExportDocument<TBlock>
{
    public string DocumentType { get; init; } = ContextualBlockContracts.DocumentType;

    public string SchemaVersion { get; init; } = "2026-06-21";

    public string BlockType { get; init; } = string.Empty;

    public ContextualBlockExportTarget Target { get; init; } = new ContextualBlockExportTarget();

    public Dictionary<string, string> Ids { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public TBlock? Block { get; init; }

    public ContextualBlockExportMetadata Metadata { get; init; } = new ContextualBlockExportMetadata();
}
