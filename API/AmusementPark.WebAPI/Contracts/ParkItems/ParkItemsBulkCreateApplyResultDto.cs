using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

public sealed class ParkItemsBulkCreateApplyResultDto
{
    public IReadOnlyCollection<ParkItemBulkCreatePreviewRowDto> Rows { get; set; } = new List<ParkItemBulkCreatePreviewRowDto>();

    public IReadOnlyCollection<string> CreatedIds { get; set; } = new List<string>();

    public int RequestedCount { get; set; }

    public int CreatedCount { get; set; }

    public int IgnoredCount { get; set; }
}
