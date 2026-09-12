using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Comments.Results;

public sealed record CommentResult(
    string Id,
    CommentTargetType TargetType,
    string TargetId,
    string AuthorUserId,
    string AuthorDisplayName,
    string? AuthorAvatarUrl,
    Role AuthorRole,
    IReadOnlyCollection<LocalizedText> Bodies,
    bool IsOfficial,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    long Revision = 0);
