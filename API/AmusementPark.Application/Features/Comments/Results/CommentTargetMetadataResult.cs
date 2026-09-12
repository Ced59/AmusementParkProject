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
