using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Queries;

/// <summary>
/// Récupère une page d'utilisateurs.
/// </summary>
/// <param name="Paging">Paramètres de pagination.</param>
public sealed record GetUsersPageQuery(PagedQuery Paging) : IQuery<ApplicationResult<PagedResult<User>>>;
