using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Contracts;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Change le mot de passe d'un utilisateur.
/// </summary>
public sealed record ChangePasswordCommand(string UserId, ChangePasswordRequest Request, bool ChangeForSelf) : ICommand<ApplicationResult>;
