using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class UserRatingListItemDto
{
    public string Id { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public string? ParkItemCategory { get; set; }

    public string? ParkItemType { get; set; }

    public double Value { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public RatingSummaryDto Summary { get; set; } = new RatingSummaryDto();
}
