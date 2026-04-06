using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Bloque un utilisateur.
/// </summary>
public sealed record LockUserCommand(string UserId) : ICommand<ApplicationResult<User>>;
