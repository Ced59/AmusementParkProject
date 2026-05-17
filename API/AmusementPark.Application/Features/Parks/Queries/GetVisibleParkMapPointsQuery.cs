using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Query de récupération des points cartographiques publics des parcs visibles.
/// </summary>
public sealed record GetVisibleParkMapPointsQuery(string? SearchTerm = null) : IQuery<ApplicationResult<IReadOnlyCollection<Park>>>;
