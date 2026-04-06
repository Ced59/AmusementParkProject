using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Contracts;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Réinitialise le mot de passe d'un utilisateur.
/// </summary>
public sealed record ResetPasswordCommand(ResetPasswordRequest Request) : ICommand<ApplicationResult>;
