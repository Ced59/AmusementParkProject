using System.Collections.Generic;
using System.Text.Json.Serialization;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? CountryCode { get; set; }

    public ParkTypeDto? Type { get; set; }

    public ParkAudienceClassificationDto? AudienceClassification { get; set; }

    public ParkStatusDto Status { get; set; } = ParkStatusDto.Operating;

    public DateTime? OpeningDate { get; set; }

    public DateTime? ClosingDate { get; set; }

    public string? OpeningDateText { get; set; }

    public string? ClosingDateText { get; set; }

    public string? FounderId { get; set; }

    public string? OperatorId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public bool IsVisible { get; set; }

    public AdminReviewStatusDto AdminReviewStatus { get; set; } = AdminReviewStatusDto.Validated;

    public bool IsFeaturedOnHome { get; set; }

    public int? FeaturedHomeOrder { get; set; }

    public bool IsFeaturedOnHomeSponsored { get; set; }

    public string? WebSiteUrl { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }

    public string? CurrentLogoImageId { get; set; }

    public int? ParkItemsTotalCount { get; set; }

    public int? ParkItemsVisibleCount { get; set; }

    public ParkOpeningHoursAdminSummaryDto? OpeningHours { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DataCompletenessScoreDto? DataCompleteness { get; set; }
}

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
