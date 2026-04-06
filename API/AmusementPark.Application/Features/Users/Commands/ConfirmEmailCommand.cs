using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Confirme l'email d'un utilisateur.
/// </summary>
public sealed record ConfirmEmailCommand(string Token) : ICommand<ApplicationResult<User>>;
