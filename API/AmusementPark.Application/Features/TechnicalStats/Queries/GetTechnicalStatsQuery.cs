using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.TechnicalStats.Contracts;

namespace AmusementPark.Application.Features.TechnicalStats.Queries;

public sealed record GetTechnicalStatsQuery()
    : IQuery<ApplicationResult<TechnicalStatsSnapshot>>;
