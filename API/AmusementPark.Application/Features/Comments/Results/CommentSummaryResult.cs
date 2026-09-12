using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Comments.Results;

public sealed record CommentSummaryResult(
    CommentTargetType TargetType,
    string TargetId,
    long CommentCount,
    string LanguageCode,
    long LanguageCommentCount,
    CommentResult? OfficialComment);
