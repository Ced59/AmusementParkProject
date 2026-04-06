using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Assigne un rôle à un utilisateur.
/// </summary>
public sealed record AssignRoleCommand(string UserId, Role Role) : ICommand<ApplicationResult<User>>;
