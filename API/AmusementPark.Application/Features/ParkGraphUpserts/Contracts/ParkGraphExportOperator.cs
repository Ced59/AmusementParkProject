using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportOperator
{
    public string Key { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? LegalName { get; init; }

    public int? FoundedYear { get; init; }

    public int? ClosedYear { get; init; }

    public ParkReferenceContactDetails? ContactDetails { get; init; }

    public List<LocalizedText> Description { get; init; } = new List<LocalizedText>();

    public AdminReviewStatus AdminReviewStatus { get; init; }
}
