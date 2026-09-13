using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

public sealed class ParkItemAdminPublicationSignalsDto
{
    public bool IsVisible { get; set; }

    public AdminReviewStatusDto AdminReviewStatus { get; set; } = AdminReviewStatusDto.Validated;

    public DateTime? LastUpdatedAtUtc { get; set; }

    public IReadOnlyCollection<string> AvailableLanguageCodes { get; set; } = Array.Empty<string>();

    public bool IsPublishable { get; set; }
}
