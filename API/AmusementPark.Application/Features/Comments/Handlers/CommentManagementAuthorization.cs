using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Commands;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Comments.Queries;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Comments.Handlers;

internal static class CommentManagementAuthorization
{
    public static async Task<User?> GetActorAsync(
        string actorUserId,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return null;
        }

        User? actor = await userRepository.GetByIdAsync(actorUserId.Trim(), cancellationToken);
        bool isAllowed = actor is not null
            && actor.IsActivated
            && !actor.IsBlocked;
        return isAllowed ? actor : null;
    }

}
