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

internal static class CommentResultFactory
{
    public static CommentResult Create(Comment comment, User? currentAuthor = null)
    {
        bool hasCurrentAuthor = currentAuthor is not null
            && string.Equals(currentAuthor.Id, comment.AuthorUserId, StringComparison.Ordinal);
        string authorDisplayName = hasCurrentAuthor
            ? currentAuthor!.ResolvePublicDisplayName() ?? comment.AuthorDisplayName
            : comment.AuthorDisplayName;
        string? authorAvatarUrl = hasCurrentAuthor
            ? NormalizeAvatarUrl(currentAuthor!.AvatarUrl)
            : comment.AuthorAvatarUrl;
        Role authorRole = hasCurrentAuthor
            ? ResolveAuthorRole(currentAuthor!)
            : comment.AuthorRole;

        return new CommentResult(
            comment.Id,
            comment.TargetType,
            comment.TargetId,
            comment.AuthorUserId,
            authorDisplayName,
            authorAvatarUrl,
            authorRole,
            comment.Bodies,
            comment.IsOfficial,
            comment.CreatedAtUtc,
            comment.UpdatedAtUtc,
            comment.Revision);
    }

    private static Role ResolveAuthorRole(User author)
    {
        if (author.HasRole(Role.Admin))
        {
            return Role.Admin;
        }

        return author.HasRole(Role.Moderator) ? Role.Moderator : Role.User;
    }

    private static string? NormalizeAvatarUrl(string? avatarUrl)
    {
        return string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
    }
}
