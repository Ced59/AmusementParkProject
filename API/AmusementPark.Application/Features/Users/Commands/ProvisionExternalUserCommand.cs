using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Application.Features.Users.Results;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Provisionne un utilisateur à partir d'une identité externe.
/// </summary>
public sealed record ProvisionExternalUserCommand(ProvisionExternalUserRequest Request) : ICommand<ApplicationResult<AuthenticatedUserResult>>;
