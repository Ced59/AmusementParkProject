using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

public sealed class ParkItemsBulkCreateRequestDto
{
    public string ParkId { get; set; } = string.Empty;

    public IReadOnlyCollection<ParkItemBulkCreateDraftDto> Rows { get; set; } = new List<ParkItemBulkCreateDraftDto>();
}
