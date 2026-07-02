using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Query de récupération des points cartographiques publics des parcs visibles.
/// </summary>
public sealed record GetVisibleParkMapPointsQuery(
    string? SearchTerm = null,
    WorldRegionFilter? Region = null,
    ParkAudienceClassificationFilter? AudienceClassificationFilter = null,
    ClosedEntityFilter ClosedFilter = ClosedEntityFilter.OpenOnly) : IQuery<ApplicationResult<IReadOnlyCollection<Park>>>;
