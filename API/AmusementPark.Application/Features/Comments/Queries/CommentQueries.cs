using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Core.Domain.Comments;

namespace AmusementPark.Application.Features.Comments.Queries;

public sealed record GetCommentSummaryQuery(
    CommentTargetType TargetType,
    string TargetId,
    bool IncludeHidden,
    string? LanguageCode = null) : IQuery<ApplicationResult<CommentSummaryResult>>;

public sealed record GetCommentThreadQuery(
    CommentTargetType TargetType,
    string TargetId,
    bool IncludeHidden) : IQuery<ApplicationResult<CommentThreadResult>>;
