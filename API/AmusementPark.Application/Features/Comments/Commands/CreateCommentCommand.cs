using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Contracts;
using AmusementPark.Application.Features.Comments.Results;

namespace AmusementPark.Application.Features.Comments.Commands;

public sealed record CreateCommentCommand(
    string AuthorUserId,
    CommentWriteModel Model) : ICommand<ApplicationResult<CommentResult>>;

public sealed record UpdateCommentCommand(
    string ActorUserId,
    string CommentId,
    CommentEditModel Model) : ICommand<ApplicationResult<CommentResult>>;

public sealed record DeleteCommentCommand(
    string ActorUserId,
    string CommentId,
    long? ExpectedRevision = null) : ICommand<ApplicationResult>;
