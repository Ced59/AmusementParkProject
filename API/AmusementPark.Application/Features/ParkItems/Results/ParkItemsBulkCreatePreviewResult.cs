using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Results;

public sealed class ParkItemsBulkCreatePreviewResult
{
    public IReadOnlyCollection<ParkItemBulkCreatePreviewRow> Rows { get; init; } = Array.Empty<ParkItemBulkCreatePreviewRow>();

    public int ReadyCount { get; init; }

    public int WarningCount { get; init; }

    public int ErrorCount { get; init; }
}
