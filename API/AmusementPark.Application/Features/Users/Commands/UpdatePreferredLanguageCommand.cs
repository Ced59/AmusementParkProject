using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Commands;

/// <summary>
/// Updates only the preferred language of the authenticated user.
/// </summary>
public sealed record UpdatePreferredLanguageCommand(string UserId, string? PreferredLanguage)
    : ICommand<ApplicationResult<User>>;
