using System.Text.Json;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public enum ParkGraphBulkParkSelectionMode
{
    Filtered,
    Explicit,
}

public sealed class ParkGraphBulkExportRequest
{
    public ParkGraphBulkParkSelectionMode SelectionMode { get; init; } = ParkGraphBulkParkSelectionMode.Filtered;

    public IReadOnlyCollection<string> ParkIds { get; init; } = Array.Empty<string>();

    public string? SearchTerm { get; init; }

    public bool? IsVisible { get; init; }

    public AdminReviewStatus? AdminReviewStatus { get; init; }

    public ParkType? Type { get; init; }

    public ParkAudienceClassificationFilter? AudienceClassificationFilter { get; init; }

    public string? CountryCode { get; init; }

    public bool? HasValidCoordinates { get; init; }

    public ClosedEntityFilter ClosedFilter { get; init; } = ClosedEntityFilter.All;

    public ParkOpeningHoursAdminFilter OpeningHoursFilter { get; init; } = ParkOpeningHoursAdminFilter.All;

    public ParkAdminSortField SortField { get; init; } = ParkAdminSortField.Default;

    public bool SortDescending { get; init; }

    public IReadOnlyCollection<ParkGraphExportSection> Sections { get; init; } = Array.Empty<ParkGraphExportSection>();
}

public sealed class BulkParkGraphUpsertRequest
{
    public bool CreateIfMissing { get; init; }

    public bool ReplaceCollections { get; init; }

    public JsonElement Document { get; init; }

    public string RawJson { get; init; } = string.Empty;
}
