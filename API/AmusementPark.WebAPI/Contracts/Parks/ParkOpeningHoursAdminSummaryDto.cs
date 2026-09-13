using System.Collections.Generic;
using System.Text.Json.Serialization;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkOpeningHoursAdminSummaryDto
{
    public bool HasOpeningHours { get; set; }

    public string Status { get; set; } = "NotConfigured";

    public string? TimeZoneId { get; set; }

    public string? FirstDate { get; set; }

    public string? LastDate { get; set; }

    public string? CompleteUntilDate { get; set; }

    public int? CompleteForDays { get; set; }

    public int WarningThresholdDays { get; set; } = 30;

    public DateTime? LastVerifiedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
