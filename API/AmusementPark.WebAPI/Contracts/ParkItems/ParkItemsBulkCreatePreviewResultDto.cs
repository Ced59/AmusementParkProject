using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

public sealed class ParkItemsBulkCreatePreviewResultDto
{
    public IReadOnlyCollection<ParkItemBulkCreatePreviewRowDto> Rows { get; set; } = new List<ParkItemBulkCreatePreviewRowDto>();

    public int ReadyCount { get; set; }

    public int WarningCount { get; set; }

    public int ErrorCount { get; set; }
}
