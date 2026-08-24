using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Queries;

public sealed record GetVisibleStandaloneAttractionMapPointsQuery(
    string? SearchTerm = null,
    WorldRegionFilter? Region = null) : IQuery<ApplicationResult<IReadOnlyCollection<StandaloneAttraction>>>;
