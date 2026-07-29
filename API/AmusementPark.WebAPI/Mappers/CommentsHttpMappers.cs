using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Comments.Contracts;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Localization;
using AmusementPark.WebAPI.Contracts.Comments;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Mappers;

internal static class CommentsHttpMappers
{
    public static CommentTargetType ToCommentTargetType(this string? value)
    {
        return Enum.TryParse(value, true, out CommentTargetType parsed) ? parsed : default;
    }

    public static CommentWriteModel ToApplication(this CreateCommentRequestDto request)
    {
        return new CommentWriteModel(
            request.TargetType.ToCommentTargetType(),
            request.TargetId,
            request.Bodies
                .Select(static body => new LocalizedTextValue(body.LanguageCode, body.Value ?? string.Empty))
                .ToList(),
            request.IsOfficial);
    }

    public static CommentEditModel ToApplication(this UpdateCommentRequestDto request)
    {
        return new CommentEditModel(
            request.Bodies
                .Select(static body => new LocalizedTextValue(body.LanguageCode, body.Value ?? string.Empty))
                .ToList(),
            request.IsOfficial);
    }

    public static CommentDto ToHttp(
        this CommentResult value,
        string? actorUserId = null,
        bool canManageAll = false)
    {
        bool canManage = canManageAll
            || (!string.IsNullOrWhiteSpace(actorUserId)
                && string.Equals(actorUserId, value.AuthorUserId, StringComparison.Ordinal));
        return new CommentDto
        {
            Id = value.Id,
            TargetType = value.TargetType.ToString(),
            TargetId = value.TargetId,
            AuthorDisplayName = value.AuthorDisplayName,
            AuthorRole = value.AuthorRole.ToString(),
            Bodies = value.Bodies.Select(ToHttp).ToList(),
            IsOfficial = value.IsOfficial,
            CanUpdate = canManage,
            CanDelete = canManage,
            CreatedAtUtc = value.CreatedAtUtc,
            UpdatedAtUtc = value.UpdatedAtUtc,
        };
    }

    public static CommentSummaryDto ToHttp(
        this CommentSummaryResult value,
        string? actorUserId = null,
        bool canManageAll = false)
    {
        return new CommentSummaryDto
        {
            TargetType = value.TargetType.ToString(),
            TargetId = value.TargetId,
            CommentCount = value.CommentCount,
            OfficialComment = value.OfficialComment?.ToHttp(actorUserId, canManageAll),
        };
    }

    public static CommentThreadDto ToHttp(
        this CommentThreadResult value,
        string? actorUserId = null,
        bool canManageAll = false)
    {
        return new CommentThreadDto
        {
            TargetType = value.TargetType.ToString(),
            TargetId = value.TargetId,
            TargetName = value.TargetName,
            ParkId = value.ParkId,
            ParkName = value.ParkName,
            Comments = value.Comments
                .Select(comment => comment.ToHttp(actorUserId, canManageAll))
                .ToList(),
        };
    }

    private static LocalizedTextDto ToHttp(LocalizedText value)
    {
        return new LocalizedTextDto
        {
            LanguageCode = value.LanguageCode,
            Value = value.Value,
        };
    }
}
