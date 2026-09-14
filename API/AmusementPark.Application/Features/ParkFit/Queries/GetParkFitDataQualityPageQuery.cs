using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Results;

namespace AmusementPark.Application.Features.ParkFit.Queries;

/// <summary>
/// Demande une page bornée d'audits FIT pour l'administration.
/// </summary>
public sealed record GetParkFitDataQualityPageQuery(PagedQuery Paging)
    : IQuery<ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>>>;
