using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportFounder
{
    public string Key { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Occupation { get; init; }

    public string? BirthDate { get; init; }

    public string? DeathDate { get; init; }

    public string? BirthPlace { get; init; }

    public string? NationalityCountryCode { get; init; }

    public string? WebsiteUrl { get; init; }

    public List<LocalizedText> Biography { get; init; } = new List<LocalizedText>();
}
