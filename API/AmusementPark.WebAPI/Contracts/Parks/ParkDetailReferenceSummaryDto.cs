using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.Ratings;

namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkDetailReferenceSummaryDto
{
    public string? FounderName { get; set; }

    public string? OperatorName { get; set; }
}
