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

public sealed class ContextualBlockExportTarget
{
    public string EntityType { get; init; } = string.Empty;

    public string EntityId { get; init; } = string.Empty;
}

public sealed class ContextualParkDescriptionBlock
{
    public string ParkId { get; init; } = string.Empty;

    public List<LocalizedText> Descriptions { get; init; } = new List<LocalizedText>();
}

public sealed class ContextualParkPracticalBlock
{
    public string ParkId { get; init; } = string.Empty;

    public string? CountryCode { get; init; }

    public string? City { get; init; }

    public string? Street { get; init; }

    public string? PostalCode { get; init; }

    public string? WebsiteUrl { get; init; }

    public string? FounderId { get; init; }

    public string? OperatorId { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}

public sealed class ContextualBlockExportMetadata
{
    public string Source { get; init; } = "admin-contextual-block-export";

    public DateTime ExportedAtUtc { get; init; }
}
