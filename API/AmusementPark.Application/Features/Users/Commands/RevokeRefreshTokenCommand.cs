using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Commande de révocation d'un refresh token opaque.
/// </summary>
public sealed record RevokeRefreshTokenCommand(string RefreshToken, string Reason) : ICommand<ApplicationResult>;
