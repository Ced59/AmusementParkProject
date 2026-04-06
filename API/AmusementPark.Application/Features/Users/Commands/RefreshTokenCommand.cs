using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Application.Features.Users.Results;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Rafraîchit les tokens d'un utilisateur.
/// </summary>
public sealed record RefreshTokenCommand(RefreshTokenRequest Request) : ICommand<ApplicationResult<AuthenticatedUserResult>>;
