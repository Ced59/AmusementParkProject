using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Comments.Results;

public sealed record CommentThreadResult(
    CommentTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName,
    IReadOnlyCollection<CommentResult> Comments);
