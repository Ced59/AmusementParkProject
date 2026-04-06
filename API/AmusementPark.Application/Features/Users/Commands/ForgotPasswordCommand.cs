using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Contracts;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Déclenche l'oubli de mot de passe.
/// </summary>
public sealed record ForgotPasswordCommand(ForgotPasswordRequest Request) : ICommand<ApplicationResult>;
