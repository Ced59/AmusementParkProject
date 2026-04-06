using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Application.Features.Users.Results;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Authentifie un utilisateur local.
/// </summary>
public sealed record LoginCommand(LoginRequest Request) : ICommand<ApplicationResult<AuthenticatedUserResult>>;
