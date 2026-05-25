using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LocalizedContent.Results;

namespace AmusementPark.Application.Features.LocalizedContent.Queries;

/// <summary>
/// Recherche de cibles localisables pour éviter le pilotage manuel par identifiant.
/// </summary>
public sealed record SearchLocalizedContentTargetsQuery(
    string EntityType,
    string? Search,
    int Page,
    int PageSize) : IQuery<ApplicationResult<PagedResult<LocalizedContentTargetResult>>>;
