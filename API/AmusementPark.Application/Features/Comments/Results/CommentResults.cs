using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Comments.Results;

public sealed record CommentTargetMetadataResult(
    CommentTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName);

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

public sealed record CommentSummaryResult(
    CommentTargetType TargetType,
    string TargetId,
    long CommentCount,
    CommentResult? OfficialComment);

public sealed record CommentThreadResult(
    CommentTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName,
    IReadOnlyCollection<CommentResult> Comments);
