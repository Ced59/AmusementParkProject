using AmusementPark.Application.Common.Contracts;
using AmusementPark.Core.Domain.Comments;

namespace AmusementPark.Application.Features.Comments.Contracts;

public sealed record CommentWriteModel(
    CommentTargetType TargetType,
    string TargetId,
    IReadOnlyCollection<LocalizedTextValue> Bodies,
    bool IsOfficial);
