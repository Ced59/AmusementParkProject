using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Queries;

/// <summary>
/// Récupère un utilisateur par email.
/// </summary>
/// <param name="Email">Email recherché.</param>
public sealed record GetUserByEmailQuery(string Email) : IQuery<ApplicationResult<User>>;
