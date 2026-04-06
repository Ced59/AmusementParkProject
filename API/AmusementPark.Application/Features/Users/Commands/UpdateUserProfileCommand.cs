using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Met à jour le profil utilisateur.
/// </summary>
public sealed record UpdateUserProfileCommand(string UserId, UserProfileUpdate Update) : ICommand<ApplicationResult<User>>;
