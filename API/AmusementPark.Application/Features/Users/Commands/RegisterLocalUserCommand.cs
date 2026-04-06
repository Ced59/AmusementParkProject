using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Inscrit un utilisateur local.
/// </summary>
public sealed record RegisterLocalUserCommand(RegisterUserRequest Request) : ICommand<ApplicationResult<User>>;
