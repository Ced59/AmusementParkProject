using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Retire un rôle à un utilisateur.
/// </summary>
public sealed record RemoveRoleCommand(string UserId, Role Role) : ICommand<ApplicationResult<User>>;
