using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Recherche des parcs par nom.
/// </summary>
/// <param name="Name">Texte recherché.</param>
/// <param name="Paging">Paramètres de pagination.</param>
/// <param name="IncludeHidden">Indique si les parcs non visibles doivent être inclus.</param>
public sealed record SearchParksByNameQuery(string Name, PagedQuery Paging, bool IncludeHidden = false) : IQuery<ApplicationResult<PagedResult<Park>>>;
