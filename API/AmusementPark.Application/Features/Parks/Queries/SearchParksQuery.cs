using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Recherche publique unifiée des parcs : nom, ville, code pays, nom localisé du pays et région.
/// </summary>
public sealed record SearchParksQuery(string? SearchTerm, WorldRegionFilter? Region, PagedQuery Paging, bool IncludeHidden = false) : IQuery<ApplicationResult<PagedResult<Park>>>;
