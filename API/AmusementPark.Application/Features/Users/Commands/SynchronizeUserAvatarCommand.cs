using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Télécharge et rattache un avatar externe à un utilisateur.
/// </summary>
public sealed record SynchronizeUserAvatarCommand(string UserId, string ImageUrl) : ICommand<ApplicationResult<User>>;
