using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Results;

public sealed class ParkItemsBulkCreateApplyResult
{
    public IReadOnlyCollection<ParkItemBulkCreatePreviewRow> Rows { get; init; } = Array.Empty<ParkItemBulkCreatePreviewRow>();

    public IReadOnlyCollection<string> CreatedIds { get; init; } = Array.Empty<string>();

    public int RequestedCount { get; init; }

    public int CreatedCount { get; init; }

    public int IgnoredCount { get; init; }
}
