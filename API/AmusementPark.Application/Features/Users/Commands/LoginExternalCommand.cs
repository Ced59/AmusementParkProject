using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Results;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Authentifie un utilisateur déjà connu via un fournisseur externe.
/// </summary>
public sealed record LoginExternalCommand(string Email) : ICommand<ApplicationResult<AuthenticatedUserResult>>;
