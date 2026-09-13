using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportPark
{
    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? CountryCode { get; init; }

    public ParkType? Type { get; init; }

    public ParkAudienceClassification? AudienceClassification { get; init; }

    public ParkStatus Status { get; init; } = ParkStatus.Operating;

    public DateTime? OpeningDate { get; init; }

    public DateTime? ClosingDate { get; init; }

    public string? OpeningDateText { get; init; }

    public string? ClosingDateText { get; init; }

    public string? FounderId { get; init; }

    public string? FounderKey { get; init; }

    public string? OperatorId { get; init; }

    public string? OperatorKey { get; init; }

    public List<LocalizedText> Descriptions { get; init; } = new List<LocalizedText>();

    public bool IsVisible { get; init; }

    public AdminReviewStatus AdminReviewStatus { get; init; }

    public bool IsFeaturedOnHome { get; init; }

    public int? FeaturedHomeOrder { get; init; }

    public bool IsFeaturedOnHomeSponsored { get; init; }

    public string? WebsiteUrl { get; init; }

    public string? Street { get; init; }

    public string? City { get; init; }

    public string? PostalCode { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

}
