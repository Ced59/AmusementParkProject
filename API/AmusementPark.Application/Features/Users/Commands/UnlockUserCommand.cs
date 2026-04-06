using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Débloque un utilisateur.
/// </summary>
public sealed record UnlockUserCommand(string UserId) : ICommand<ApplicationResult<User>>;
