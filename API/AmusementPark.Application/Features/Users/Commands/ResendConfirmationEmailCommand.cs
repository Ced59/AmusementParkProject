using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Renvoie un email de confirmation.
/// </summary>
public sealed record ResendConfirmationEmailCommand(string Email) : ICommand<ApplicationResult>;
