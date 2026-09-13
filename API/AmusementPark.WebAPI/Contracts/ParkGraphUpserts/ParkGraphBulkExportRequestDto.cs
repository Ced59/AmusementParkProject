using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ParkGraphUpserts;

public sealed class ParkGraphBulkExportRequestDto
{
    public string SelectionMode { get; set; } = "filtered";

    public List<string> ParkIds { get; set; } = new List<string>();

    public string? SearchTerm { get; set; }

    public bool? IsVisible { get; set; }

    public string? AdminReviewStatus { get; set; }

    public string? Type { get; set; }

    public string? AudienceClassification { get; set; }

    public string? CountryCode { get; set; }

    public bool? HasValidCoordinates { get; set; }

    public string? ClosedFilter { get; set; }

    public string? OpeningHoursStatus { get; set; }

    public string? SortBy { get; set; }

    public string? SortDirection { get; set; }

    public List<string> Sections { get; set; } = new List<string>();
}
