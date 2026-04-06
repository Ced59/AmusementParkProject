using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Queries;

/// <summary>
/// Récupère un utilisateur par identifiant.
/// </summary>
/// <param name="UserId">Identifiant recherché.</param>
public sealed record GetUserByIdQuery(string UserId) : IQuery<ApplicationResult<User>>;
